namespace Raft.Transports
{
    public enum MessageTypes
    {
        VoteRequest,
        VoteReply,
        AppendEntriesReply,
        AppendEntriesRequest,
        AddServerRequest,
        AddServerReply,
        RemoveServerRequest,
        RemoveServerReply,
        EntryRequest,
        EntryRequestReply,
    }
}
