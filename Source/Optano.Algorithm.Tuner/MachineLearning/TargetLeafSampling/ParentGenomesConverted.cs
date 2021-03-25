#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2021 OPTANO GmbH
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

namespace Optano.Algorithm.Tuner.MachineLearning.TargetLeafSampling
{
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;

    /// <summary>
    /// The converted parent genomes.
    /// </summary>
    public class ParentGenomesConverted
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentGenomesConverted"/> class.
        /// </summary>
        /// <param name="competitiveParent">
        /// The competitive parent.
        /// </param>
        /// <param name="nonCompetitiveParent">
        /// The non competitive parent.
        /// </param>
        public ParentGenomesConverted(GenomeDoubleRepresentation competitiveParent, GenomeDoubleRepresentation nonCompetitiveParent)
        {
            this.CompetitiveParent = competitiveParent;
            this.NonCompetitiveParent = nonCompetitiveParent;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the competitive parent.
        /// </summary>
        public GenomeDoubleRepresentation CompetitiveParent { get; }

        /// <summary>
        /// Gets the non competitive parent.
        /// </summary>
        public GenomeDoubleRepresentation NonCompetitiveParent { get; }

        /// <summary>
        /// Gets the length of double representation.
        /// </summary>
        public int LengthOfDoubleRepresentation => this.CompetitiveParent.Length;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Gets the parent to follow.
        /// </summary>
        /// <param name="computeFixationFor">
        /// The fixation to check.
        /// </param>
        /// <returns>
        /// The <see cref="GenomeDoubleRepresentation"/>.
        /// </returns>
        public GenomeDoubleRepresentation GetParentToFollow(TargetLeafGenomeFixation computeFixationFor)
        {
            return computeFixationFor == TargetLeafGenomeFixation.FixedToCompetitiveParent
                       ? this.CompetitiveParent
                       : this.NonCompetitiveParent;
        }

        /// <summary>
        /// Gets the parent that should not be followed.
        /// </summary>
        /// <param name="computeFixationFor">
        /// The current fixation.
        /// </param>
        /// <returns>
        /// The <see cref="GenomeDoubleRepresentation"/>.
        /// </returns>
        public GenomeDoubleRepresentation GetOtherParent(TargetLeafGenomeFixation computeFixationFor)
        {
            return computeFixationFor == TargetLeafGenomeFixation.FixedToCompetitiveParent
                       ? this.NonCompetitiveParent
                       : this.CompetitiveParent;
        }

        #endregion
    }
}