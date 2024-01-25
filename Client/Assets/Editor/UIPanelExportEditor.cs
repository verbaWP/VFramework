
using System;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using CNC;
using CNC.MVC.View;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPanelExportEditor : EditorWindow
{
    private static string _sPathViewConfig    = "Assets/Scripts/Framework/MVC/ViewPathConfig.cs"; // ViewPathConfig文件路径
    // private static string _sPathViewAutoGen   = "Assets/Scripts/UI/AutoGen/";                     // 自动生成UI代码的目录
    private static string _sPathViewAutoGen    = "Assets/Scripts/Module/AutoGen/";                         // UI代码的目录
    private static string _sPathViewModule    = "Assets/Scripts/Module/";                         // UI代码的目录
    private static string _sPathPrefixRaw     = "Assets/Art/UI/PrefabsEditor/";                  // 原始UI资源目录
    private static string _sPathPrefixRuntime = "Assets/AssetsPackage/UI/PrefabsRuntime/";       // 导出后UI资源目录

    private static string _sPrefabName;
    private static string _sPrefabPathRaw;     // _Editor后缀的prefab路径
    private static string _sPrefabPathRuntime; // 导出后的prefab路径
    private static bool _sCanSafeExport;       // 用于限制美术导出
    private static UIPanelExportEditor _sWindow;       // 用于限制美术导出

    private readonly string _sPrefixExport = "m_"; // 导出物件命名前缀
    private Dictionary<string, Dictionary<string, List<string>>> _dicNameToPath = new(); // 各prefab组件名-路径信息
    private Vector2 _posScrollView;
    private List<string> _listNameNew = new();
    private List<string> _listNameDel = new();
    private List<string> _listInvalidImageInfo = new();
    private List<string> _listPrefabSuccess = new();
    private List<string> _listPrefabChanged = new();
    private List<string> _listPrefabFailed = new();

    // 界面配置
    private bool _isPop;                // 是否为弹出界面
    private bool _isPersistent;         // 是否常驻不销毁（需配合BaseContainer一起使用
    private bool _isDestroyImmediately; // 是否立即销毁


    [MenuItem("Assets/Tool/程序导出Prefab &c", false, 33)]
    static void ExportFromClient()
    {
        if (!checkValid()) return;

        _sCanSafeExport = true;
        _sWindow = GetWindow<UIPanelExportEditor>($"UI导出");
        _sWindow.Show();
    }

    [MenuItem("Assets/Tool/美术导出Prefab", false, 33)]
    static void ExportFromArt()
    {
        if (!checkValid()) return;

        _sCanSafeExport = false;
        _sWindow = GetWindow<UIPanelExportEditor>($"UI导出");
        _sWindow.Show();
    }

    private static bool checkValid()
    {
        GameObject tObj = Selection.activeGameObject;
        if (!tObj) return false;
        _sPrefabPathRaw = AssetDatabase.GetAssetPath(tObj);
        _sPrefabName = Path.GetFileNameWithoutExtension(_sPrefabPathRaw);
        _sPrefabPathRuntime = getPrefabRuntimePath(_sPrefabPathRaw.Replace("\\", "/"));
        if (!_sPrefabPathRaw.Contains(_sPathPrefixRaw))
        {
            EditorUtility.DisplayDialog("警告", $"只能能导出'{_sPathPrefixRaw}'下资源:\n当前路径:{_sPrefabPathRaw}", "确定");
            return false;
        }
        // 根节点检查
        GameObject tObjRaw = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(_sPrefabPathRaw));
        if (tObjRaw.GetComponent<UIRootSection>() == null)
        {
            EditorUtility.DisplayDialog("提示", $"UI根节点必须挂载'UIRootSection'或'UIRootPanel'", "确定");
            GameObject.DestroyImmediate(tObjRaw);
            return false;
        }
        GameObject.DestroyImmediate(tObjRaw);
        return true;
    }

    private static string getPrefabRuntimePath(string srcPath)
    {
        return srcPath.Replace(_sPathPrefixRaw, _sPathPrefixRuntime).Replace("_Editor", "");
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("文件名", GUILayout.Width(100));
        GUILayout.TextField(_sPrefabName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("路径", GUILayout.Width(100));
        GUILayout.TextField(_sPrefabPathRaw);
        GUILayout.EndHorizontal();

        _posScrollView = GUILayout.BeginScrollView(_posScrollView);
        bool tHasInvalid = false;
        GUIStyle errorStyle = new GUIStyle();
        errorStyle.fontSize = 20;
        errorStyle.normal.textColor = Color.red;
        GUIStyle warningStyle = new GUIStyle();
        warningStyle.fontSize = 20;
        warningStyle.normal.textColor = Color.yellow;

        // 首次导出配置
        if (CheckIsFirstExportCode(out var _))
        {
            GUILayout.Space(16);
            GUILayout.Label("首次导出UI代码配置：");
            GUILayout.BeginVertical("box");
            {
                _isPop = GUILayout.Toggle(_isPop, "是否为弹出界面");
                _isPersistent = GUILayout.Toggle(_isPersistent, "是否常驻不销毁（需配合BaseContainer一起使用");
                if (_isPersistent) _isDestroyImmediately = false;
                _isDestroyImmediately = GUILayout.Toggle(_isDestroyImmediately, "是否立即销毁");
                if (_isDestroyImmediately) _isPersistent = false;
            }
            GUILayout.EndVertical();
        }

        // 存在同名节点不能导出
        if (_dicNameToPath.ContainsKey(_sPrefabPathRaw) == false)
        {
            _dicNameToPath[_sPrefabPathRaw] = new Dictionary<string, List<string>>();
            checkDuplicatedNodeName(_sPrefabPathRaw, _dicNameToPath[_sPrefabPathRaw]);
            checkNewDelNode(_sPrefabPathRaw, _sPrefabPathRuntime);
            checkImages(_sPrefabPathRaw);
        }
        var prefabShortNameDict = _dicNameToPath[_sPrefabPathRaw];
        int duplicatedCount = 0;
        foreach (var pair in prefabShortNameDict)
        {
            if (pair.Value.Count > 1)
                duplicatedCount++;
        }
        if (duplicatedCount > 0)
        {
            GUILayout.Space(20);
            GUILayout.Label("以下节点有重名", errorStyle);
            foreach (var pair in prefabShortNameDict)
            {
                if (pair.Value.Count > 1)
                {
                    GUILayout.BeginHorizontal();
                    foreach (var path in pair.Value)
                        GUILayout.TextField(path, errorStyle);
                    GUILayout.EndHorizontal();
                    tHasInvalid = true;
                }
            }
        }
        // 图片引用异常不能导出
        if (_listInvalidImageInfo.Count > 0)
        {
            tHasInvalid = true;
            GUILayout.Space(20);
            GUILayout.Label("以下图片引用有异常");
            foreach (var item in _listInvalidImageInfo)
                GUILayout.TextField(item, errorStyle);
        }

        // 新加节点
        if (_listNameNew.Count > 0)
        {
            GUILayout.Space(20);
            GUILayout.Label("新增以下节点");
            foreach (var item in _listNameNew)
                GUILayout.TextField(item, warningStyle);
        }
        // 删除节点
        if (_listNameDel.Count > 0)
        {
            GUILayout.Space(30);
            GUILayout.Label("删除以下节点");
            foreach (var item in _listNameDel)
                GUILayout.TextField(item, warningStyle);
        }
        GUILayout.EndScrollView();

        GUILayout.Space(30);
        if (!tHasInvalid && GUILayout.Button("开始转换 + 自动生成UI代码"))
        {
            bool tPreBlock = !_sCanSafeExport && (_listNameNew.Count > 0 || _listNameDel.Count > 0);
            if(tPreBlock) EditorUtility.DisplayDialog("转换失败", $"{_sPrefabName} 结构有改动，请联系相应程序操作", "确定");
            exportPrefab(_sPrefabPathRaw, _sCanSafeExport);
        }
    }

    private bool exportPrefab(string srcPath, bool isSafeExport)
    {
        GameObject tObj = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(srcPath));
        DirectoryInfo info = Directory.GetParent(srcPath);
        string exportDir = creatParentDir(info);

        string fileName = tObj.name.Replace("_Editor(Clone)", "") + ".prefab";
        string newPath = exportDir + "/" + fileName;
        bool isNewFile = !File.Exists(newPath);

        UIEffectData[] effectDatas = tObj.GetComponentsInChildren<UIEffectData>(true);
        for (int i = 0; i < effectDatas.Length; i++)
        {
            var data = effectDatas[i].parmDatas;
            int c = data.Length;
            effectDatas[i].parmTypes = new int[c];
            effectDatas[i].loadTypes = new bool[c];
            effectDatas[i].parms = new string[c];

            for (int j = 0; j < data.Length; j++)
            {
                string path = string.Empty;
                if (string.IsNullOrEmpty(path))
                {
                    path = CNC.Editor.UIEditorHelper.GetPrefabAssetPathAfter2017(data[j].go);
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogError("特效子节点不是Prefab:" + effectDatas[i].gameObject.name);
                        continue;
                    }
                }

                if (srcPath == path)
                {
                    Debug.LogError("特效节点不能是自己:" + effectDatas[i].gameObject.name);
                }
                else
                {
                    effectDatas[i].parmTypes[j] = (int) data[j].bindParmType;
                    effectDatas[i].parms[j] = path.Replace("\\", "/").Replace("Assets/AssetsPackage/", string.Empty);
                    effectDatas[i].loadTypes[j] = data[j].loadOnUse;
                }
            }
        }

        UIAudioData[] audioDatas = tObj.GetComponentsInChildren<UIAudioData>(true);
        for (int i = 0; i < audioDatas.Length; i++)
        {
            audioDatas[i].parmTypes = new int[] { (int) UIBindParmType.Audio };
            audioDatas[i].parms = new string[] { audioDatas[i].audioEvent };
            audioDatas[i].audioEvent = null;
        }

        UIGoPool[] goPools = tObj.GetComponentsInChildren<UIGoPool>(true);
        List<string> parentPaths = new List<string>();
        for (int i = 0; i < goPools.Length; i++)
        {
            Dictionary<string, PrefabExportType> HandleDic = new Dictionary<string, PrefabExportType>();
            foreach (var item in goPools[i].List)
            {
                if (null == item.Prefab)
                {
                    Debug.LogError(goPools[i].gameObject.name + " gopool 存在空的对象引用!");
                    continue;
                }
                HandleDic[item.Prefab.name] = (PrefabExportType) item.ExportType;
            }

            foreach (var item in HandleDic)
            {
                if (item.Value == PrefabExportType.Group)
                {
                    var gogo = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(goPools[i].gameObject);
                    if (null == gogo) gogo = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(goPools[i].gameObject);
                    var baseParent = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(goPools[i]);
                    string ph = null == gogo ? "" : getPathToRoot(gogo.transform, baseParent.transform);
                    parentPaths.Add(ph);
                }
            }
        }

        // 创建新prefab
        var newPrefab = GameObject.Instantiate(tObj) as GameObject;
        UIEffectData[] instanceEffectDatas = newPrefab.GetComponentsInChildren<UIEffectData>(true);
        for (int i = 0; i < instanceEffectDatas.Length; i++)
        {
            var data = instanceEffectDatas[i].parmDatas;
            // int c = data.Length;
            for (int j = 0; j < data.Length; j++)
            {
                var effect = instanceEffectDatas[i].transform.Find(data[j].go.name);
                if (null != effect)
                {
                    GameObject.DestroyImmediate(effect.gameObject);
                }
            }
            instanceEffectDatas[i].parmDatas = null;
        }

        disposeTableView(newPrefab);

        UIGoPool[] instanceGoPools = newPrefab.GetComponentsInChildren<UIGoPool>(true);
        for (int i = instanceGoPools.Length - 1; i >= 0; i--)
        {
            {
                int c = 0;
                foreach (var item in instanceGoPools[i].List)
                {
                    if ((PrefabExportType) item.ExportType == PrefabExportType.Group)
                    {
                        string parentPath = parentPaths[c++];
                        var parent = newPrefab.transform.Find(parentPath).Find("m_groupPoolTransf");
                        if (null == parent)
                        {
                            parent = new GameObject("m_groupPoolTransf", new System.Type[1] { typeof(RectTransform) }).transform;
                            parent.SetParent(newPrefab.transform.Find(parentPath));
                            parent.localPosition = Vector3.zero;
                            parent.localScale = Vector3.one;
                            parent.localRotation = Quaternion.identity;
                            parent.gameObject.SetActive(false);
                        }

                        var go = item.Prefab;
                        if (null != go)
                        {
                            go.name = item.Name;
                            go.transform.SetParent(parent);
                        }
                    }
                }
            }

            UnityEngine.Object.DestroyImmediate(instanceGoPools[i]);
        }
        var allTransf = newPrefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransf.Length; i++)
        {
            var transf = allTransf[i];
            if (transf.name.Contains("") || transf.name.Contains("_editor"))
            {
                transf.name = string.Join("", Regex.Split(transf.name, "_Editor", RegexOptions.IgnoreCase));
                transf.name = string.Join("", Regex.Split(transf.name, "_editor", RegexOptions.IgnoreCase));
            }
        }

        // 导出代码
        if (isSafeExport) exportCode(newPrefab, isNewFile);
        _sWindow.Close();

        AssetDatabase.DeleteAsset(newPath);
        PrefabUtility.SaveAsPrefabAsset(newPrefab, newPath);
        AssetDatabase.Refresh();
        GameObject.DestroyImmediate(newPrefab);
        GameObject.DestroyImmediate(tObj);

        EditorUtility.DisplayDialog("转换成功", $"文件如下：\n{_sPrefabName}", "确定");
        return isNewFile;
    }

    bool isNeedExport(Transform node)
    {
        return node.name.StartsWith(_sPrefixExport);
    }

    string getPathToRoot(Transform ctrl, Transform root)
    {
        string totalName = string.Empty;
        while (ctrl != root)
        {
            totalName = ctrl.name + (string.IsNullOrEmpty(totalName) ? "" : "/") + totalName;
            ctrl = ctrl.parent;
        }
        return totalName;
    }

    void getAllShortName(Transform root, HashSet<string> nameSet)
    {
        nameSet.Clear();

        RectTransform[] rtfs = root.GetComponentsInChildren<RectTransform>(true);
        foreach (var rtf in rtfs)
        {
            if (isNeedExport(rtf)) {
                nameSet.Add(rtf.name);
            }
        }
    }

    void checkDuplicatedNodeName(string pathRaw, Dictionary<string, List<string>> shortNameDict)
    {
        shortNameDict.Clear();
        GameObject tObjRaw = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(pathRaw));
        RectTransform[] rtfs = tObjRaw.transform.GetComponentsInChildren<RectTransform>(true);
        foreach (var rtf in rtfs)
        {
            if (isNeedExport(rtf))
            {
                string ctrlName = rtf.name;
                if (shortNameDict.ContainsKey(ctrlName) == false)
                    shortNameDict[ctrlName] = new List<string>();
                shortNameDict[ctrlName].Add(getPathToRoot(rtf, tObjRaw.transform));
            }
        }
        GameObject.DestroyImmediate(tObjRaw);
    }

    void checkNewDelNode(string pathRaw, string pathRuntime)
    {
        _listNameNew.Clear();
        _listNameDel.Clear();

        HashSet<string> layoutNameSet = new HashSet<string>();
        HashSet<string> uguiNameSet = new HashSet<string>();

        GameObject tObjRaw = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(pathRaw));
        getAllShortName(tObjRaw.transform, layoutNameSet);
        GameObject.DestroyImmediate(tObjRaw);

        if (File.Exists(pathRuntime))
        {
            GameObject tObjRuntime = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(pathRuntime)) as GameObject;
            getAllShortName(tObjRuntime.transform, uguiNameSet);
            GameObject.DestroyImmediate(tObjRuntime);
        }

        foreach (var item in layoutNameSet)
        {
            if (uguiNameSet.Contains(item) == false)
                _listNameNew.Add(item);
        }
        foreach (var item in uguiNameSet)
        {
            if (layoutNameSet.Contains(item) == false)
                _listNameDel.Add(item);
        }
    }

    private void checkImages(string pathRaw)
    {
        _listInvalidImageInfo.Clear();
        GameObject tObjRaw = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(pathRaw));
        var imgs = tObjRaw.transform.GetComponentsInChildren<Image>(true);
        Image img;
        for (int i = 0; i < imgs.Length; i++)
        {
            img = imgs[i];
            if (img.sprite != null)
            {
                var sprPath = AssetDatabase.GetAssetPath(img.sprite);
                if (sprPath.Contains("/LoadImages/"))
                {
                    _listInvalidImageInfo.Add($"【{img.name}】Image.sprite引用了LoadImages里的图片");
                }
                if (sprPath.Contains("/Images/"))
                {
                    _listInvalidImageInfo.Add($"【{img.name}】引用旧的图集资源");
                }
            }

            if (img.SpriteList != null)
            {
                foreach (var spr in img.SpriteList)
                {
                    if (spr != null)
                    {
                        var sprPath = AssetDatabase.GetAssetPath(spr);
                        if (sprPath.Contains("/LoadImages/"))
                        {
                            _listInvalidImageInfo.Add($"【{img.name}】Image.spriteList引用了LoadImages里的图片，Index:【{i}】");
                        }

                        if (sprPath.Contains("/Images/"))
                        {
                            _listInvalidImageInfo.Add($"【{img.name}】在SpriteList中引用旧的图集资源，Index:【{i}】");
                        }
                    }
                }

            }
        }
        GameObject.DestroyImmediate(tObjRaw);
    }

    private string creatParentDir(DirectoryInfo info)
    {
        string path = info.FullName;
        path = path.Replace(@"\", "/");
        path = path.Replace("Assets/Art/UI/PrefabsEditor/", "Assets/AssetsPackage/UI/PrefabsRuntime/");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private void disposeTableView(GameObject newPrefab)
    {
        var tableView = newPrefab.GetComponentsInChildren<TableView>(true);
        for (int i = 0; i < tableView.Length; i++)
        {
            var tv = tableView[i];
            if (tv)
            {
                var srv = tv.GetComponent<ScrollRect>();
                if (srv && srv.content)
                {
                    var lg = srv.content.GetComponent<LayoutGroup>();
                    var CSF = srv.content.GetComponent<ContentSizeFitter>();
                    if (lg)
                    {
                        tv.padding = lg.padding;
                        tv.top = lg.padding.top;
                        tv.bottom = lg.padding.bottom;
                        tv.left = lg.padding.left;
                        tv.right = lg.padding.right;

                        if (lg is HorizontalOrVerticalLayoutGroup)
                        {

                            tv.m_cellInterval = (lg as HorizontalOrVerticalLayoutGroup).spacing;
                        }
                        else
                        {
                            var spac = (lg as GridLayoutGroup).spacing;
                            if (srv.horizontal)
                            {
                                tv.m_cellInterval = spac.x;
                                tv.m_unitCellInterval = spac.y;
                            }
                            else
                            {
                                tv.m_cellInterval = spac.y;
                                tv.m_unitCellInterval = spac.x;
                            }
                        }
                        GameObject.DestroyImmediate(lg);
                    }
                    if (CSF)
                    {
                        GameObject.DestroyImmediate(CSF);
                    }
                }
            }
        }
    }

    private bool exportCode(GameObject objRoot, bool isNew)
    {
        var tUIRoot = objRoot.GetComponent<UIRootSection>();
        if (tUIRoot == null) return false;

        Dictionary<UIBehaviour, string> tDicNode = new Dictionary<UIBehaviour, string>();
        RectTransform[] tArr = objRoot.GetComponentsInChildren<RectTransform>(true);
        UIBehaviour tUINode;
        foreach (var item in tArr)
        {
            tUINode = getMainUIBehaviour(item);
            if (tUINode == null) continue;
            if(isNeedExport(item)) tDicNode.Add(tUINode, item.name);
        }

        List<SortEntry> sortedIndexList = new List<SortEntry>();
        foreach (var item in tDicNode)
        {
            string memberName = item.Value;

            // 去除数字
            string onlyAlpha = Regex.Replace(memberName, @"\d", "");
            SortEntry se = new SortEntry(memberName, onlyAlpha, item.Key);
            sortedIndexList.Add(se);
        }
        sortedIndexList.Sort(sortUINode);
        // 标记同前缀名组件
        for (int i = 0; i < sortedIndexList.Count - 1; ++i)
        {
            if (sortedIndexList[i].artName.Equals(sortedIndexList[i + 1].artName))
            {
                sortedIndexList[i].inUse = true;
                sortedIndexList[i + 1].inUse = true;
            }
        }
        // 排序后加进UIRootSection
        for (int i = 0; i < sortedIndexList.Count; ++i)
            tUIRoot.RegNode(sortedIndexList[i].ui);

        createCodeConfig(isNew);
        createCodeView(sortedIndexList, isNew, tUIRoot);
        return true;
    }

    private void createCodeConfig(bool isNew)
    {
        // if (!isNew) return; // 非首次生成直接返回
        string tTxt = @"// auto generated , not Edit
using System.Collections.Generic;

public class ViewPathConfig
{{
    public static Dictionary<string, string> dicPrefab = new Dictionary<string, string>()
    {{
{0}
    }};
}}
";
        string tStrItem = "        {{\"{0}\", \"{1}\"}},\n", tContent = "";
        string tNameFile = _sPrefabName.Replace("_Editor", ""), tNamePrefab = _sPrefabPathRuntime.Replace(_sPathPrefixRuntime, "");
        var tDic = ViewPathConfig.dicPrefab ?? new Dictionary<string, string>();
        var tKeys = tDic.Keys.ToList();
        if (tKeys.IndexOf(tNameFile) == -1) tKeys.Add(tNameFile);
        tKeys.Sort();
        foreach (var item in tKeys)
        {
            tContent += string.Format(tStrItem, item, item != tNameFile ? tDic[item] : tNamePrefab);
        }

        writeFile(_sPathViewConfig, string.Format(tTxt, tContent));
    }

    private void createCodeView(List<SortEntry> srcList, bool isNew, UIRootSection uiRoot)
    {
        string tNameFile = _sPrefabName.Replace("_Editor", "");
        // 写入UIHelperXxxPanel.cs
        string tTxtAutoGen = @"// auto generated , not Edit
using CNC;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class {0}
{{
    private UIRootSection _uiRoot; // ui对应的prefab根节点

{1}
{2}
    public {0}(GameObject objRoot)
    {{
        _uiRoot = objRoot.GetComponent<UIRootSection>();
{3}
    }}

    public UIBehaviour GetUINode(int nodeIndex)
    {{
        return _uiRoot.GetNode(nodeIndex);
    }}
}}
";
        string tTemplateIndex      = "    public int {0} = {1};\n";
        string tTemplateNode       = "    public {0} {1};\n";
        string tTemplateAssignment = "        {0} = _uiRoot.GetNode({1}) as {2};\n";
        string tStrIndex = "", tStrNode = "", tStrAssignment = "", tNameHelper = $"UIHelper{tNameFile}";
        SortEntry tItem;
        for (int i = 0; i < srcList.Count; i++)
        {
            tItem = srcList[i];
            // 写入索引
            if (tItem.inUse)
            {
                tStrIndex += string.Format(tTemplateIndex, tItem.name.Insert(2, "Idx"), i);
            }
            // 写入组件声明
            tStrNode += string.Format(tTemplateNode, tItem.ui.GetType().Name, tItem.name);
            // 写入组件初始化
            tStrAssignment += string.Format(tTemplateAssignment, tItem.name, i, tItem.ui.GetType().Name);
        }
        writeFile($"{_sPathViewAutoGen}{tNameHelper}.cs", string.Format(tTxtAutoGen, tNameHelper, tStrIndex, tStrNode, tStrAssignment));

        // 写入XxxPanel.cs
        if (!CheckIsFirstExportCode(out var tPath)) return; // 仅首次写入
        // CheckIsFirstExportCode(out var tPath); // 仅首次写入 -- test
        string tTxtClass = @"using System;
using System.Collections.Concurrent;
using CNC.MVC.View;

public class {0}: {1}
{{
    private {2} _uiHelper;
{3}
    // public void _OnAwake(int arg1, string arg2) {{ }}
    protected override void _OnInit()
    {{
        base._OnInit();
        _uiHelper = new {2}(_gameObject);
{4}
    }}
    // protected override void _OnEnable()
    // {{
    //     base._OnEnable();
    // }}
    // protected override void _OnShow()
    // {{
    //     base._OnShow();
    // }}
    // protected override void _OnDisable()
    // {{
    //     base._OnDisable();
    // }}

    protected override void _OnExit(bool isDestroy)
    {{
{5}
        base._OnExit(isDestroy);
    }}

    public override ConcurrentDictionary<string, Delegate> ListNotification()
    {{
        return new ConcurrentDictionary<string, Delegate>()
        {{
            // [TestGlobals.TestView_Key3] = (Action<int, string>)TestMethod3,
        }};
    }}

{6}
}}
";
        // 属性覆盖
        string tTemplateConstructor = @"
    public {0}()
    {{
{1}
    }}
";
        string tTemplateProperty       = "        {0} = true;\n";

        string tTemplateAddListener    = "        _uiHelper.{0}.onClick.AddListener({1});\n";    // 添加事件
        string tTemplateRemoveListener = "        _uiHelper.{0}.onClick.RemoveListener({1});\n"; // 移除事件
        string tTemplateListener       = "    private void {0}() {{ }}\n";
        string tStrProperty = "", tStrAddListener = "", tStrRemoveListener = "", tStrListener = "";
        string tFuncName;
        for (int i = 0; i < srcList.Count; i++)
        {
            tItem = srcList[i];
            // 界面属性（构造函数内，可以为空）
            // // test
            // _isPop = true;
            // _isDestroyImmediately = true;
            if (_isPop || _isPersistent || _isDestroyImmediately)
            {
                if (_isPop) tStrProperty += string.Format(tTemplateProperty, nameof(_isPop));
                if (_isPersistent) tStrProperty += string.Format(tTemplateProperty, nameof(_isPersistent));
                if (_isDestroyImmediately) tStrProperty += string.Format(tTemplateProperty, nameof(_isDestroyImmediately));
                tStrProperty = tStrProperty.Substring(0, tStrProperty.Length - 1); // 移除最后一个换行符
                tStrProperty = string.Format(tTemplateConstructor, tNameFile, tStrProperty);
            }
            if (tItem.ui is Button)
            {
                tFuncName = $"_On{tItem.name.Substring(2)}";
                // 添加事件
                tStrAddListener += string.Format(tTemplateAddListener, tItem.name, tFuncName);
                // 移除事件
                tStrRemoveListener += string.Format(tTemplateRemoveListener, tItem.name, tFuncName);
                // 事件函数
                tStrListener += string.Format(tTemplateListener, tFuncName);
            }
        }
        string tNameBaseClass;
        if (uiRoot is UIRootPanel)
            tNameBaseClass = (uiRoot as UIRootPanel).isContainer ? nameof(BaseContainer) : nameof(BasePanel);
        else tNameBaseClass = nameof(BaseSection);
        writeFile(tPath, string.Format(tTxtClass, tNameFile, tNameBaseClass, tNameHelper, tStrProperty, tStrAddListener, tStrRemoveListener, tStrListener));
    }

    private bool CheckIsFirstExportCode(out string pathOutput)
    {
        string tPath = _sPrefabPathRuntime.Replace(_sPathPrefixRuntime, "").Replace(".prefab", ".cs"); // 模块+文件名
        tPath = tPath.Replace("\\", "/");
        pathOutput = string.Format("{0}{1}", _sPathViewModule, tPath.Insert(tPath.IndexOf("/", StringComparison.Ordinal), "/View"));
        return !File.Exists(pathOutput);
    }

    private static UIBehaviour getMainUIBehaviour(Transform trans)
    {
        if (trans.GetComponent<Selectable>() != null)
            return trans.GetComponent<Selectable>();
        else if (trans.GetComponent<UIBehaviour>() != null)
            return trans.GetComponent<UIBehaviour>();
        else
            return null;
    }

    private void writeFile(string path, string txt)
    {
        if (File.Exists(path))
            File.Delete(path);

        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        StreamWriter streamWriter = new StreamWriter(fileStream);
        streamWriter.WriteLine(txt);
        streamWriter.Flush();
        streamWriter.Close();
        fileStream.Close();
    }

    private int sortUINode(SortEntry itemA, SortEntry itemB)
    {
        // 根据以下的规则排序，使同类控件排在一起
        // 1. 去掉数字后的名字长度
        // 2. 去掉数字后的字母表
        // 3. 原始名字长度
        // 4. 原始名字的字母表

        if (itemA.artlen == itemB.artlen)
        {
            if (itemA.artName == itemB.artName)
            {
                if (itemA.len == itemB.len)
                    return itemA.name.CompareTo(itemB.name);
                else
                    return itemA.len.CompareTo(itemB.len);
            }
            else
                return itemA.artName.CompareTo(itemB.artName);
        }
        else
            return itemA.artlen.CompareTo(itemB.artlen);
    }

    class SortEntry
    {
        public SortEntry(string _name, string _artName, UIBehaviour _ui = null)
        {
            ui = _ui;
            name = _name;
            artName = _artName;
            len = name.Length;
            artlen = artName.Length;
            inUse = false;
        }
        public string name;
        public string artName;
        public int len;
        public int artlen;
        public bool inUse;
        public UIBehaviour ui;
    }

}
