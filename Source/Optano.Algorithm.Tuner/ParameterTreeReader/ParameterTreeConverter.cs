#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
//        ALL RIGHTS RESERVED.
// 
//    The entire contents of this file is protected by German and
//    International Copyright Laws. Unauthorized reproduction,
//    reverse-engineering, and distribution of all or any portion of
//    the code contained in this file is strictly prohibited and may
//    result in severe civil and criminal penalties and will be
//    prosecuted to the maximum extent possible under the law.
// 
//    RESTRICTIONS
// 
//    THIS SOURCE CODE AND ALL RESULTING INTERMEDIATE FILES
//    ARE CONFIDENTIAL AND PROPRIETARY TRADE SECRETS OF
//    OPTANO GMBH.
// 
//    THE SOURCE CODE CONTAINED WITHIN THIS FILE AND ALL RELATED
//    FILES OR ANY PORTION OF ITS CONTENTS SHALL AT NO TIME BE
//    COPIED, TRANSFERRED, SOLD, DISTRIBUTED, OR OTHERWISE MADE
//    AVAILABLE TO OTHER INDIVIDUALS WITHOUT WRITTEN CONSENT
//    AND PERMISSION FROM OPTANO GMBH.
// 
// ////////////////////////////////////////////////////////////////////////////////

#endregion

namespace Optano.Algorithm.Tuner.ParameterTreeReader
{
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.ParameterTreeReader.Elements;

    /// <summary>
    /// Converts XML files respecting the schema defined by parameterTree.xsd into <see cref="ParameterTree" />s.
    /// </summary>
    public static class ParameterTreeConverter
    {
        #region Public Methods and Operators

        /// <summary>
        /// Tries to convert the XML document located at the provided path into a <see cref="ParameterTree" />.
        /// </summary>
        /// <param name="pathToXmlDocument">Path to XML document to convert into <see cref="ParameterTree" />.</param>
        /// <returns>The extracted <see cref="ParameterTree" /> or null if the extraction was unsuccessful.
        /// </returns>
        public static ParameterTree ConvertToParameterTree(string pathToXmlDocument)
        {
            // Try to load XML file.
            XDocument xmlDocument;
            if (!TryLoadXmlDocument(pathToXmlDocument, out xmlDocument))
            {
                return null;
            }

            // Check if it uses the correct schema.
            if (!ValidateXmlDocumentAgainstSchema(xmlDocument))
            {
                return null;
            }

            // Translate XML into helper classes if it does.
            Node simpleTree;
            using (var reader = xmlDocument.CreateReader())
            {
                simpleTree = (Node)new XmlSerializer(typeof(Node)).Deserialize(reader);
            }

            // Finally try and translate it into a functional parameter tree.
            ParameterTree tree;
            try
            {
                tree = new ParameterTree(simpleTree.ConvertToParameterTreeNode());
                return tree;
            }
            catch (XmlException e)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Error when translating into parameter tree: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Tries to load the XML document via <see cref="XDocument.Load(string)"/>.
        /// </summary>
        /// <param name="pathToXmlDocument">Path to the XML document.</param>
        /// <param name="xmlFile">Will either be set to the loaded XML document or null if an
        /// <see cref="XmlException"/> is encountered.</param>
        /// <returns>Whether the document was loaded successfully.</returns>
        private static bool TryLoadXmlDocument(string pathToXmlDocument, out XDocument xmlFile)
        {
            try
            {
                xmlFile = XDocument.Load(pathToXmlDocument);
            }
            catch (XmlException e)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Error when loading XML document {pathToXmlDocument}: {e.Message}");
                xmlFile = null;
                return false;
            }
            catch (IOException e)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Error when trying to open document {pathToXmlDocument}: {e.Message}");
                xmlFile = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the provided document against the XSD defined for parameter trees. Writes errors to console.
        /// </summary>
        /// <param name="xmlDocument">The document to validate.</param>
        /// <returns>True iff the file matches the schema.</returns>
        private static bool ValidateXmlDocumentAgainstSchema(XDocument xmlDocument)
        {
            bool isValid = true;

            // Load schema.
            var schemas = new XmlSchemaSet();
            schemas.Add("", PathUtils.GetAbsolutePathFromExecutableFolderRelative("parameterTree.xsd"));

            // Check validity of file.
            xmlDocument.Validate(
                schemas,
                validationEventHandler: (sender, error) =>
                    {
                        // Write any problems to console.
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Warn,
                            $"Error when validating XML against schema: {error.Message}");
                        isValid = false;
                    });

            return isValid;
        }

        #endregion
    }
}