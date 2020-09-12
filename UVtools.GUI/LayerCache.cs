using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace UVtools.GUI
{
    public sealed class LayerCache
    {
        private Mat _image;
        private Array _layerHierarchyJagged;
        private VectorOfVectorOfPoint _layerContours;
        private Mat _layerHierarchy;

        public Mat Image
        {
            get => _image;
            set
            {
                Clear();
                _image = value;
            }
        }

        public VectorOfVectorOfPoint LayerContours
        {
            get
            {
                if (_layerContours is null) CacheContours();
                return _layerContours;
            }
            private set => _layerContours = value;
        }

        public Mat LayerHierarchy
        {
            get
            {
                if (_layerHierarchy is null) CacheContours();
                return _layerHierarchy;
            }
            private set => _layerHierarchy = value;
        }

        public Array LayerHierarchyJagged
        {
            get
            {
                if(_layerHierarchyJagged is null) CacheContours();
                return _layerHierarchyJagged;
            }
            private set => _layerHierarchyJagged = value;
        }

        public void CacheContours(bool refresh = false)
        {
            if(refresh) Clear();
            if (!ReferenceEquals(_layerContours, null)) return;
            _layerContours = new VectorOfVectorOfPoint();
            _layerHierarchy = new Mat();
            CvInvoke.FindContours(Image, _layerContours, _layerHierarchy, RetrType.Ccomp,
                ChainApproxMethod.ChainApproxSimple);
            _layerHierarchyJagged = _layerHierarchy.GetData();
        }
        

        /// <summary>
        /// Clears the cache
        /// </summary>
        public void Clear()
        {
            _layerContours?.Dispose();
            _layerContours = null;
            _layerHierarchy?.Dispose();
            _layerHierarchy = null;
            _layerHierarchyJagged = null;
        }
    }
}
