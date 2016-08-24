﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xigadee
{
    public class PersistenceExpressionHelper<E>
    {
        PropertyMap mPropertyMap;

        public PersistenceExpressionHelper()
        {
            mPropertyMap = GetPropertyMap();
        }

        /// <summary>
        /// This is root property map.
        /// </summary>
        protected class PropertyMap
        {
            public PropertyMap(Type rootType)
            {

            }

            public Type RootType { get; }
        }

        private static PropertyMap GetPropertyMap(Type type = null)
        {
            if (type == null)
                type = typeof(E);

            var map = new PropertyMap(type);

            var properties = type.GetProperties().Where(p => p.CanRead);

            return map;
        }
    }
}
