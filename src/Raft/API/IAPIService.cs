using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace Raft.API
{
    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface IDataService
    {
        [OperationContract]
        //[WebInvoke(UriTemplate = "upload")]
        FileIndex UploadFile(RemoteStream stream);

        [OperationContract]
        //[WebInvoke(UriTemplate = "download")]
        RemoteStream DownloadFile(FileIndex index);
    }

}
