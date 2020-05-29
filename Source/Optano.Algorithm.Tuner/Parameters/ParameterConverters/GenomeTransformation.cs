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

namespace Optano.Algorithm.Tuner.Parameters.ParameterConverters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// Class that handles the encoding from <see cref="Genome"/> to <see cref="T:double[]"/> for a given <see cref="ParameterTree"/>.
    /// </summary>
    /// <typeparam name="TCategoricalEncoding">
    /// The categorical encoder.
    /// </typeparam>
    public class GenomeTransformation<TCategoricalEncoding> : IGenomeTransformation
        where TCategoricalEncoding : CategoricalEncodingBase, new()
    {
        #region Fields

        /// <summary>
        /// The transformation and encoding lock.
        /// </summary>
        private readonly object _transformationAndEncodingLock = new object();

        /// <summary>
        /// The feature lengths.
        /// </summary>
        private int[] _featureLengths;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeTransformation{TCategoricalEncoding}"/> class.
        /// </summary>
        /// <param name="tree">
        /// The parameter tree.
        /// </param>
        public GenomeTransformation(ParameterTree tree)
        {
            this.KnownTransformations = new Dictionary<Genome, double[]>(new Genome.GeneValueComparator());
            this.EncodedCategories = new Dictionary<string, object>();
            this.CategoricalEncoding = new TCategoricalEncoding();

            this.Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            this.ParameterTreeNodeOrder = Comparer<IParameterNode>.Create(
                (paramLeft, paramRight) => string.Compare(paramLeft.Identifier, paramRight.Identifier, StringComparison.Ordinal));
            this.OrderedTreeNodes = this.Tree.GetParameters(this.ParameterTreeNodeOrder).ToArray();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the feature count required to represent the <see cref="Tree"/> as <see cref="T:double[]"/>.
        /// </summary>
        public int FeatureCount
        {
            get
            {
                return this.GetFeatureLengths().Sum(l => l);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the parameter tree this <see cref="GenomeTransformation{TCategoricalEncoder}"/> works with.
        /// </summary>
        protected ParameterTree Tree { get; }

        /// <summary>
        /// Gets a comparer that defines the order in which the nodes are sorted. Double array representation will use this ordering.
        /// Can be null.
        /// </summary>
        protected Comparer<IParameterNode> ParameterTreeNodeOrder { get; }

        /// <summary>
        /// Gets or sets the <typeparamref name="TCategoricalEncoding"/>.
        /// </summary>
        protected TCategoricalEncoding CategoricalEncoding { get; set; }

        /// <summary>
        /// Gets or sets the cached categorical encodings.
        /// </summary>
        protected Dictionary<string, object> EncodedCategories { get; set; }

        /// <summary>
        /// Gets the parameter nodes in the tree ordered by Identifier.
        /// Ordering is the same as in the <see cref="F64Matrix"/>. 
        /// Keep in mind that there might be index shifts to the right, 
        /// since categorical nodes might be represented by more than 1 column.
        /// </summary>
        protected IParameterNode[] OrderedTreeNodes { get; }

        /// <summary>
        /// Gets the known transformations.
        /// </summary>
        protected Dictionary<Genome, double[]> KnownTransformations { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Converts the given <paramref name="genome"/> into the respective <see cref="T:double[]"/> representation.
        /// Categorical features are converted using the specified <typeparamref name="TCategoricalEncoding"/>.
        /// </summary>
        /// <param name="genome">
        /// The genome to convert.
        /// </param>
        /// <returns>
        /// The genome values. Orderd by IParameterNode.Identifier.
        /// </returns>
        public double[] ConvertGenomeToArray(Genome genome)
        {
            // convert genome to double[].
            var doubleRep = this.ConvertGenome(genome);

            // Make a shallow copy so that nobody can mess around with the stored representation.
            return (double[])doubleRep.Clone();
        }

        /// <summary>
        /// Converts the given <see cref="T:double[]"/> encoding back into a <see cref="Genome"/>.
        /// </summary>
        /// <param name="encodedGenome">
        /// <see cref="T:double[]"/> representation of a genome.
        /// </param>
        /// <returns>
        /// Representation of the <paramref name="encodedGenome"/> as <see cref="Genome"/> object.
        /// </returns>
        public Genome ConvertBack(double[] encodedGenome)
        {
            // only reading access -> no need to lock in this method.
            var restored = new Genome();

            // careful: OrderedTreeNodes only has #features many entires.
            // the double representation may have more! we need separate indices.
            var orderedFeatureIndex = 0;
            var featureIndexInDoubleRep = 0;
            while (featureIndexInDoubleRep < encodedGenome.Length)
            {
                var identifier = this.OrderedTreeNodes[orderedFeatureIndex].Identifier;
                var featureDomain = this.OrderedTreeNodes[orderedFeatureIndex].Domain;

                // if IsCategoricalDomain, we will need to skip more than 1 column. columnsToSkip will be set accordingly
                var columnsToSkip = 1;
                if (featureDomain.IsCategoricalDomain)
                {
                    var featureLength = this.CategoricalEncoding.NumberOfGeneratedColumns(featureDomain);
                    var featureKey = new double[featureLength];
                    for (var i = 0; i < featureLength; i++)
                    {
                        featureKey[i] = encodedGenome[i + featureIndexInDoubleRep];
                    }

                    var convertedCategory = this.EncodeNodeCategory(identifier, (dynamic)featureDomain);
                    var domValue = convertedCategory.GetDomainValueAsAllele(featureKey);

                    restored.SetGene(identifier, domValue);

                    // the following number of columns do not need to be handled
                    columnsToSkip = featureLength;
                }
                else
                {
                    var allele = featureDomain.ConvertBack(encodedGenome[featureIndexInDoubleRep]);
                    restored.SetGene(identifier, allele);
                }

                // increment for upcoming features. 'featureIndexInDoubleRep' may be incremented by more than 1.
                orderedFeatureIndex++;
                featureIndexInDoubleRep += columnsToSkip;
            }

            return restored;
        }

        /// <summary>
        /// Get the feature lengths.
        /// </summary>
        /// <returns>
        /// The <see cref="IReadOnlyList{Integer}"/>, containing the number of columns required to represent a feature.
        /// </returns>
        public IReadOnlyList<int> GetFeatureLengths()
        {
            if (this._featureLengths == null)
            {
                var featureLengths = new int[this.OrderedTreeNodes.Length];
                for (var featureIndex = 0; featureIndex < this.OrderedTreeNodes.Length; featureIndex++)
                {
                    var featureDomain = this.OrderedTreeNodes[featureIndex].Domain;
                    var featureLength = featureDomain.IsCategoricalDomain ? this.CategoricalEncoding.NumberOfGeneratedColumns(featureDomain) : 1;

                    featureLengths[featureIndex] = featureLength;
                }

                this._featureLengths = featureLengths;
            }

            return this._featureLengths;
        }

        /// <summary>
        /// Restore the internal dictionaries with correct comparers.
        /// </summary>
        public void RestoreInternalDictionariesWithCorrectComparers()
        {
            foreach (var encodedCategory in this.EncodedCategories.Values)
            {
                var cast = encodedCategory as IConvertedCategory;
                cast?.Initialize();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Tries to retrieve the cached encoding for the given <paramref name="category"/>, or encodes + stores it in <see cref="EncodedCategories"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <paramref name="category"/> domain.
        /// </typeparam>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <param name="category">
        /// The encoding.
        /// </param>
        /// <returns>
        /// The <see cref="ConvertedCategory{T}"/>.
        /// </returns>
        protected IConvertedCategory EncodeNodeCategory<T>(string identifier, CategoricalDomain<T> category)
        {
            // only lock on the affected dictionary. this is the only block form which dictionary is accessed.
            lock (this._transformationAndEncodingLock)
            {
                if (this.EncodedCategories.ContainsKey(identifier))
                {
                    return this.EncodedCategories[identifier] as IConvertedCategory;
                }

                // only local variables/reading access in CategoricalEncoding. No need to lock in there.
                var encoding = this.CategoricalEncoding.Encode(identifier, category);
                this.EncodedCategories.Add(identifier, encoding);
                return encoding;
            }
        }

        /// <summary>
        /// Converts the given <paramref name="genome"/> into the respective double-representation.
        /// Categorical features are converted using the specified <typeparamref name="TCategoricalEncoding"/>.
        /// Caches the transformation for each genome, so that they do not need to be re-computed.
        /// <c>Make sure to call result.Clone(), when exposing the return value to public.</c>.
        /// </summary>
        /// <param name="genome">
        /// The genome to convert.
        /// </param>
        /// <returns>
        /// The genome values. Ordered by IParameterNode.Identifier. 
        /// </returns>
        protected double[] ConvertGenome(Genome genome)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            // prevent duplicate creation + adding of genome conversion
            lock (this._transformationAndEncodingLock)
            {
                if (this.KnownTransformations.ContainsKey(genome))
                {
                    return this.KnownTransformations[genome];
                }

                // leave some additional space for possible categorical parameter columns -> init with 1.5 * length
                var columnValues = new List<double>((int)(this.OrderedTreeNodes.Length * 1.5));

                // add the required number of columns (with respective value) to the resulting double[]
                foreach (var paramNode in this.OrderedTreeNodes)
                {
                    // let it throw, if genome is not valid for this tree.
                    var parameter = genome.GetGeneValue(paramNode.Identifier);
                    var domain = paramNode.Domain;

                    if (domain.IsCategoricalDomain)
                    {
                        var enc = this.EncodeNodeCategory(paramNode.Identifier, (dynamic)domain);
                        double[] columns = enc.GetColumnRepresentation(parameter.GetValue());
                        columnValues.AddRange(columns);
                    }
                    else
                    {
                        var column = domain.ConvertToDouble(parameter);
                        columnValues.Add(column);
                    }
                }

                // store known transformation
                var transformation = columnValues.ToArray();
                this.KnownTransformations.Add(genome, transformation);

                return transformation;
            }
        }

        #endregion
    }
}