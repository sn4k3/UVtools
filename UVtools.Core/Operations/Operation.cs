/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using System.Runtime.CompilerServices;
using Emgu.CV;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public abstract class Operation
    {
        public const byte ClassNameLength = 9;

        /// <summary>
        /// Gets the ID name of this operation, this comes from class name with "Operation" removed
        /// </summary>
        public string Id => GetType().Name.Remove(0, ClassNameLength);

        /// <summary>
        /// Gets if this operation should set layer range to the actual layer index on layer preview
        /// </summary>
        public virtual bool PassActualLayerIndex => false;

        /// <summary>
        /// Gets the title of this operation
        /// </summary>
        public virtual string Title => Id;

        /// <summary>
        /// Gets a descriptive text of this operation
        /// </summary>
        public virtual string Description => Id;

        /// <summary>
        /// Gets the Ok button text
        /// </summary>
        public virtual string ButtonOkText => Title;

        /// <summary>
        /// Gets the confirmation text for the operation
        /// </summary>
        public virtual string ConfirmationText => $"Are you sure you want to {Id}";

        /// <summary>
        /// Gets the progress window title
        /// </summary>
        public virtual string ProgressTitle => "Processing items";

        /// <summary>
        /// Gets the progress action name
        /// </summary>
        public virtual string ProgressAction => Id;

        /// <summary>
        /// Validates the operation
        /// </summary>
        /// <returns>null or empty if validates, or else, return a string with error message</returns>
        public virtual StringTag Validate(params object[] parameters) => null;

        public bool CanValidate(params object[] parameters)
        {
            var result = Validate(parameters);
            return result is null || string.IsNullOrEmpty(result.Content);
        }

        /// <summary>
        /// Gets the start layer index where operation will starts in
        /// </summary>
        public virtual uint LayerIndexStart { get; set; }

        /// <summary>
        /// Gets the end layer index where operation will ends in
        /// </summary>
        public virtual uint LayerIndexEnd { get; set; }

        public uint LayerRangeCount => LayerIndexEnd - LayerIndexStart + 1;

        /// <summary>
        /// Gets or sets an ROI to process this operation
        /// </summary>
        public Rectangle ROI { get; set; } = Rectangle.Empty;

        public bool HaveROI => !ROI.IsEmpty;

        public Mat GetRoiOrDefault(Mat defaultMat)
        {
            return HaveROI ? new Mat(defaultMat, ROI) : defaultMat;
        }
    }
}
