/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
namespace UVtools.Core.Operations
{
    public class Operation
    {
        const byte ClassNameLength = 9;

        /// <summary>
        /// Gets the ID name of this operation, this comes from class name with "Operation" removed
        /// </summary>
        public string Id => GetType().Name.Remove(0, ClassNameLength);
    }
}
