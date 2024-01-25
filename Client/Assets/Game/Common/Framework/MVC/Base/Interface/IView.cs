// author:蓝涣
// create time:20231225

namespace CNC.MVC.Base.Interfaces
{
    public interface IView: INotifier
    {
        /// <summary>
        ///  判断界面是否已销毁
        /// </summary>
        bool IsDestroyed();

        /// <summary>
        /// 设置是否可见
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        /// <returns>是否有效操作。未加载完成或已销毁为无效</returns>
        bool SetVisible(bool isVisible);
    }
}
