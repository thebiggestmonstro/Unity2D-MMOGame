using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game.Job
{
    public interface IJob
    {
        void Execute();
    }

    // 인자가 없는 패킷을 저장하는 Job
    public class Job : IJob
    {
        Action _action;

        // 생성자에서 패킷을 받아 저장
        public Job(Action action)
        {
            _action = action;
        }

        // 인자가 없는 패킷 처리 요청
        public void Execute() 
        { 
            _action.Invoke();
        }
    }

    // 인자를 1개 사용하는 패킷을 저장하는 Job
    public class Job<T1> : IJob
    {
        Action<T1> _action;
        T1 _t1;

        // 생성자에서 패킷을 받아 저장
        public Job(Action<T1> action, T1 t1)
        {
            _action = action;
            _t1 = t1;
        }

        // 인자가 1개 있는 패킷 처리 요청
        public void Execute()
        {
            _action.Invoke(_t1);
        }
    }

    // 인자를 2개 사용하는 패킷을 저장하는 Job
    public class Job<T1, T2> : IJob
    {
        Action<T1, T2> _action;
        T1 _t1;
        T2 _t2;

        // 생성자에서 패킷을 받아 저장
        public Job(Action<T1, T2> action, T1 t1, T2 t2)
        {
            _action = action;
            _t1 = t1;
            _t2 = t2;
        }

        // 인자가 2개 있는 패킷 처리 요청
        public void Execute()
        {
            _action.Invoke(_t1, _t2);
        }
    }

    // 인자를 3개 사용하는 패킷을 저장하는 Job
    public class Job<T1, T2, T3> : IJob
    {
        Action<T1, T2, T3> _action;
        T1 _t1;
        T2 _t2;
        T3 _t3;

        // 생성자에서 패킷을 받아 저장
        public Job(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            _action = action;
            _t1 = t1;
            _t2 = t2;
            _t3 = t3;
        }

        // 인자가 3개 있는 패킷 처리 요청
        public void Execute()
        {
            _action.Invoke(_t1, _t2, _t3);
        }
    }
}
