del delta /q
del bravo /q
del charlie /q
start raft create --ip=127.0.0.1:7001 --data=delta
start raft join --ip=127.0.0.1:7002 --data=bravo --leader=127.0.0.1:7001
start raft join --ip=127.0.0.1:7003 --data=charlie --leader=127.0.0.1:7001
ping 0.0.0.0 -n 2
start raft agent --ip=127.0.0.1:7001 --data=delta --agentip=127.0.0.1:8001
ping 0.0.0.0 -n 2
start raft agent --ip=127.0.0.1:7002 --data=bravo --agentip=127.0.0.1:8002
start raft agent --ip=127.0.0.1:7003 --data=charlie --agentip=127.0.0.1:8003