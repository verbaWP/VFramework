// author:蓝涣
// create time:20231225

using UnityEngine;
using UnityEngine.UI;


public class GameUtil {

    //查找指定名称的节点
    public static GameObject FindGameobject(GameObject sObj, string sName) {
        if (!sObj) return null;
        Transform result = null;

        Transform tParent = sObj.transform;
        result = tParent.Find(sName);
        if (result) return result.gameObject;

        // 遍历子节点
        GameObject tObj;
        int tLength = tParent.childCount;
        for (int i = 0; i < tLength; i++) {
            tObj = FindGameobject(tParent.GetChild(i).gameObject, sName);
            if (tObj) return tObj;
        }

        //foreach (Transform item in tParent) {
        //    Debugger.Log("------------------------------- child name>>>" + item.name + "--- parent name>>>" + tParent.name);
        //}

        return null;
    }

    //设置激活状态
    public static void SetActive(object srcObj, bool isActive) {
        GameObject tObj = null;
        if (srcObj is Component) tObj = (srcObj as Component).gameObject;
        else if (srcObj is GameObject) tObj = srcObj as GameObject;

        if (!tObj) {
#if DEBUG
            Debug.LogError("---------------------------------- param 'srcObj' is null when call 'SetActive'");
#endif
            return;
        }

        tObj.SetActive(isActive);
    }

    //设置可见状态
    public static void SetVisible(Object srcObj, bool isVisible, bool isImage = false) {
        GameObject tObj = null;
        if (srcObj is Component) tObj = (srcObj as Component).gameObject;
        else if (srcObj is GameObject) tObj = srcObj as GameObject;
        if (!tObj) {
#if DEBUG
            Debug.LogError("---------------------------------- param 'srcObj' is null when call 'SetVisible'");
#endif
            return;
        }

        if (isImage) {
            CanvasRenderer tCvsRender = tObj.GetComponent<CanvasRenderer>();
            if (!tCvsRender) return;
            tCvsRender.SetAlpha(isVisible ? 1 : 0);
        } else {
            RectTransform tTran = tObj.transform as RectTransform;
            Vector3 tPos = tTran.anchoredPosition3D;
            tPos.z = isVisible ? 0 : 10000;
            tTran.anchoredPosition3D = tPos;
        }
    }

    // 设置layer属性
    public static void SetLayer(GameObject srcObj, int layer, bool recursive = true) {
        if (null == srcObj) return;

        if (recursive) {
            Transform tParent = srcObj.transform;
            int tLength = tParent.childCount;
            for (int i = 0; i < tLength; i++) SetLayer(tParent.GetChild(i).gameObject, layer);
        } else srcObj.layer = layer;
    }

    // 调整sortingOrder属性，没有Canvas组件的添加该组件
    public static void AdjustPanelSorting(GameObject srcObj, int baseOrder) {
        if (null == srcObj) return;
        Canvas tCvs = srcObj.GetComponent<Canvas>();
        if (!tCvs) {
            tCvs = srcObj.AddComponent<Canvas>();
            srcObj.AddComponent<GraphicRaycaster>();
            tCvs.overrideSorting = true;
        }
        tCvs.sortingOrder = baseOrder;
    }

    public static void SetParent(GameObject srcObj, GameObject parent, bool isReset = false, bool isFullScreen = false) {
        if (srcObj == null || parent == null) return;

        Transform tTrans = srcObj.transform;
        tTrans.SetParent(parent.transform, false);

        if (isReset == true) {
            tTrans.localPosition = Vector3.zero;
            tTrans.localRotation = Quaternion.identity;
            tTrans.localScale = Vector3.one;
        }
        if (isFullScreen) {
            RectTransform tRectTrans = srcObj.transform as RectTransform;
            tRectTrans.anchorMin = Vector2.zero;
            tRectTrans.anchorMax = new Vector2(1, 1);
            tRectTrans.offsetMin = Vector2.zero;
            tRectTrans.offsetMax = Vector2.zero;
        }
    }

    // 设置slibing index
    // index 目标索引。支持负数，-1表示最上层
    public static void SetChildIndex(GameObject srcObj, int index) {
        if (!srcObj) return;

        Transform tTransP = srcObj.transform.parent;
        int tLength = tTransP.childCount;
        index = index >= 0 ? index : tLength + index;
        srcObj.transform.SetSiblingIndex(index);
    }

    public static void SetMainCameraEnabled(bool isEnable)
    {

    }
}
