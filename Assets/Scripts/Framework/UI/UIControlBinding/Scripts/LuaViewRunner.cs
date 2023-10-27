using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if XLUA
using XLua;
#endif

namespace SkierFramework
{
    public class LuaViewRunner : MonoBehaviour, IBindableUI
    {
        public string viewClassName { get; set; }
#if XLUA
        public LuaTable luaUI { get; private set; }

        public LuaTable BindLua(string viewClassName)
        {
            this.viewClassName = viewClassName;

            // TODO
            return null;
        }
#endif

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
