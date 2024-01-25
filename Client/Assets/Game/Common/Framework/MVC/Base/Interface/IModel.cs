// author:蓝涣
// create time:20231225

namespace CNC.MVC.Base.Interfaces
{
    public interface IModel: INotifier
    {
        void ResetData();

        IData Data { get; set; }
    }
}
