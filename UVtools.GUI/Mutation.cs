/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;
using UVtools.Core;


namespace UVtools.GUI
{
    public class Mutation
    {
        #region Properties
        public LayerManager.Mutate Mutate { get; }

        public string MenuName { get; }
        public string Description { get; }

        public Image Image { get; }
        public Image MenuImage { get; }
        #endregion

        #region Constructor

        public Mutation(LayerManager.Mutate mutate, string menuName, Image menuImage, string description, Image image = null)
        {
            Mutate = mutate;
            MenuName = menuName ?? mutate.ToString();
            Description = description;
            MenuImage = menuImage ?? Properties.Resources.filter_filled_16x16;
        }

        #endregion
    }
}
