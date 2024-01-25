// author:蓝涣
// create time:20231225

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CNC
{
    public class UIRootSection : MonoBehaviour
    {
        [SerializeField]
        protected List<UIBehaviour> nodeList = new();

#if UNITY_EDITOR
        public void RegNode(UIBehaviour ui)
        {
            nodeList.Add(ui);
        }
#endif
        public UIBehaviour GetNode(int index)
        {
            if (index < 0 || index >= nodeList.Count)
                return null;
            return nodeList[index];
        }
    }
}
