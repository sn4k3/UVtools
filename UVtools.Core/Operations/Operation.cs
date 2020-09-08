/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.Core.Obects;

namespace UVtools.Core.Operations
{
    public class Operation
    {
        const byte ClassNameLength = 9;

        /// <summary>
        /// Gets the ID name of this operation, this comes from class name with "Operation" removed
        /// </summary>
        public string Id => GetType().Name.Remove(0, ClassNameLength);

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
        /// Validates the operation
        /// </summary>
        /// <returns>null or empty if validates, or else, return a string with error message</returns>
        public virtual StringTag Validate(params object[] parameters) => null;
    }
}
