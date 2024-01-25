// author:蓝涣
// create time:20231225

using CNC.MVC.Base;
using CNC.MVC.Base.Interfaces;

namespace CNC.MVC.Model
{
    public class BaseModel: Notifer, IModel
    {
        public BaseModel(IData data = null)
        {
            this.Data = data;
        }

        public virtual void ResetData()
        {
            // this.Data?.ResetData();
            if(this.Data != null) this.Data.ResetData();
        }
        public IData Data { get; set; }
    }
}
