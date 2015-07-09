using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{

    //////An AsyncResult that completes as soon as it is instantiated.
    ////internal class CompletedAsyncResult : AsyncResult
    ////{
    ////    public CompletedAsyncResult(AsyncCallback callback, object state)
    ////        : base(callback, state)
    ////    {
    ////        Complete(true);
    ////    }

    ////    public static void End(IAsyncResult result)
    ////    {
    ////        AsyncResult.End<CompletedAsyncResult>(result);
    ////    }
    ////}

    ////internal class CompletedAsyncResult<T> : AsyncResult
    ////{
    ////    private T _data;

    ////    public CompletedAsyncResult(T data, AsyncCallback callback, object state)
    ////        : base(callback, state)
    ////    {
    ////        _data = data;
    ////        Complete(true);
    ////    }

    ////    public static T End(IAsyncResult result)
    ////    {
    ////        CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
    ////        return completedResult._data;
    ////    }
    ////}

    ////internal class CompletedAsyncResult<TResult, TParameter> : AsyncResult
    ////{
    ////    private TResult _resultData;
    ////    private TParameter _parameter;

    ////    public CompletedAsyncResult(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
    ////        : base(callback, state)
    ////    {
    ////        _resultData = resultData;
    ////        _parameter = parameter;
    ////        Complete(true);
    ////    }

    ////    public static TResult End(IAsyncResult result, out TParameter parameter)
    ////    {
    ////        CompletedAsyncResult<TResult, TParameter> completedResult = AsyncResult.End<CompletedAsyncResult<TResult, TParameter>>(result);
    ////        parameter = completedResult._parameter;
    ////        return completedResult._resultData;
    ////    }
    ////}
    //[ServiceContract(SessionMode = SessionMode.NotAllowed)]
    //public interface INodeProxy
    //{
    //    [OperationContractAttribute(IsOneWay = true, Action = "VoteRequest")]
    //    void VoteRequest(VoteRequest request);

    //    [OperationContractAttribute(IsOneWay = true, Action = "VoteReply")]
    //    void VoteReply(VoteReply reply);

    //    [OperationContractAttribute(IsOneWay = true, Action = "AppendEntriesRequest")]
    //    void AppendEntriesRequest(AppendEntriesRequest request);

    //    [OperationContractAttribute(IsOneWay = true, Action = "AppendEntriesReply")]
    //    void AppendEntriesReply(AppendEntriesReply reply);
    //}

    //[ServiceContract(SessionMode = SessionMode.NotAllowed)]
    //public interface INodeProxyAsync : INodeProxy
    //{
    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteRequest")]
    //    IAsyncResult BeginVoteRequest(VoteRequest request, AsyncCallback callback, object asyncState);

    //    void EndVoteRequest(IAsyncResult r);

    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteReply")]
    //    IAsyncResult BeginVoteReply(VoteReply reply, AsyncCallback callback, object asyncState);

    //    void EndVoteReply(IAsyncResult r);

    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesRequest")]
    //    IAsyncResult BeginAppendEntriesRequest(AppendEntriesRequest request, AsyncCallback callback, object asyncState);

    //    void EndAppendEntriesRequest(IAsyncResult r);

    //    [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesReply")]
    //    IAsyncResult BeginAppendEntriesReply(AppendEntriesReply request, AsyncCallback callback, object asyncState);

    //    void EndAppendEntriesReply(IAsyncResult r);
    //}


    ////Factory class for client proxy
    //public abstract class ClientFactory
    //{
    //    public static INodeProxyAsync CreateClient(Type targetType)
    //    {
    //        BasicHttpBinding binding = new BasicHttpBinding();
    //        //Get the address of the service from configuration or some other mechanism - Not shown here
    //        EndpointAddress address = new EndpointAddress("http://localhost:7741/CategoryServiceHost.svc");

    //        var factory3 = new ChannelFactory<INodeProxyAsync>(binding, address);
    //        return factory3.CreateChannel();
    //    }
    //}
}
