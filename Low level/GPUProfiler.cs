using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.Windows;

namespace CSharpRenderer
{
    // Not thread-safe!!! Use only from 1 thread and immediateContext!

    static class GPUProfiler
    {
        const int MAX_HW_QUERIES = 1024*64;
        const int MAX_HW_DISJOINT_QUERIES = 8;
        static List<Query> m_HWQueries;
        static List<String> m_HWQueriesDescs;
        static List<Query> m_DisjointQueries;
        static List<int> m_CorrespondingQueryEnds;
        static Stack<int>  m_QueriesStack;
        static int m_CurrentDisjointQuery;
        static int m_CurrentQuery;
        static int m_CurrentFrameFirstQuery;

        struct PendingFrameQueries
        {
            public int m_DisjointQueryId;
            public int m_BeginQuery;
            public int m_EndQuery;
        }

        public class ProfilerTreeMember
        {
            public List<ProfilerTreeMember> m_ChildMembers;
            public ProfilerTreeMember m_Parent;
            public String m_Name;
            public Double m_Time;

            public ProfilerTreeMember()
            {
                m_ChildMembers = new List<ProfilerTreeMember>();
                m_Parent = null;
                m_Name = "invalid";
                m_Time = 0.0;
            }
        }

        public static ProfilerTreeMember m_CurrentFrameProfilerTree;


        static Queue<PendingFrameQueries> m_PendingFrames;

        static GPUProfiler()
        {
        }

        public static void Initialize(Device device)
        {
            m_HWQueries = new List<Query>();
            m_HWQueriesDescs = new List<String>();
            m_QueriesStack = new Stack<int>();
            m_DisjointQueries = new List<Query>();
            m_CorrespondingQueryEnds = new List<int>();

            m_PendingFrames = new Queue<PendingFrameQueries>();

            QueryDescription queryDesc = new QueryDescription(QueryType.Timestamp, QueryFlags.None);
            for (int i = 0; i < MAX_HW_QUERIES; ++i )
            {
                m_HWQueries.Add(new Query(device, queryDesc));
                m_HWQueriesDescs.Add("");
                m_CorrespondingQueryEnds.Add(0);
            }

            queryDesc = new QueryDescription(QueryType.TimestampDisjoint, QueryFlags.None);

            for (int i = 0; i < MAX_HW_DISJOINT_QUERIES; ++i)
            {
                m_DisjointQueries.Add(new Query(device, queryDesc));
            }
        }

        public static void BeginProfilePoint(DeviceContext context, String profilePointName)
        {
            context.End(m_HWQueries[m_CurrentQuery]);
            m_HWQueriesDescs[m_CurrentQuery] = profilePointName;
            m_QueriesStack.Push(m_CurrentQuery);

            IncrementCurrentQuery();
        }

        public static void EndProfilePoint(DeviceContext context)
        {
            context.End(m_HWQueries[m_CurrentQuery]);
            int beginQuery = m_QueriesStack.Pop();
            m_CorrespondingQueryEnds[beginQuery] = m_CurrentQuery;
            IncrementCurrentQuery();
        }

        static void IncrementCurrentQuery()
        {
            m_CorrespondingQueryEnds[m_CurrentQuery] = Int32.MaxValue;
            m_CurrentQuery++;
            m_CurrentQuery %= MAX_HW_QUERIES;
        }

        static void IncrementCurrentDisjointQuery()
        {
            m_CurrentDisjointQuery++;
            m_CurrentDisjointQuery %= MAX_HW_DISJOINT_QUERIES;
        }

        public static void BeginFrameProfiling(DeviceContext context)
        {
            context.Begin(m_DisjointQueries[m_CurrentDisjointQuery]);
            m_CurrentFrameFirstQuery = m_CurrentQuery;
            BeginProfilePoint(context, "WholeFrame");
        }

        public static void EndFrameProfiling(DeviceContext context)
        {
            PendingFrameQueries pendingFrame = new PendingFrameQueries();

            pendingFrame.m_DisjointQueryId = m_CurrentDisjointQuery;
            pendingFrame.m_BeginQuery = m_CurrentFrameFirstQuery;
            pendingFrame.m_EndQuery = m_CurrentQuery;

            EndProfilePoint(context);

            if(m_QueriesStack.Count != 0)
            {
                throw new Exception("Wrong profile point count! Did you forget about EndProfilePoint?");
            }

            context.End(m_DisjointQueries[m_CurrentDisjointQuery]);

            m_PendingFrames.Enqueue(pendingFrame);

            IncrementCurrentDisjointQuery();
            
            // Time to fetch prev frames!
            if (m_PendingFrames.Count > 4)
            {
                pendingFrame = m_PendingFrames.Dequeue();
                TimestampQueryData disjointData = context.GetData<TimestampQueryData>(m_DisjointQueries[pendingFrame.m_DisjointQueryId]);
                ProfilerTreeMember parent = new ProfilerTreeMember();
                ProfilerTreeMember frameParent = parent;

                for (int queryIterator = pendingFrame.m_BeginQuery; queryIterator != (pendingFrame.m_EndQuery + 1) % MAX_HW_QUERIES; queryIterator = (queryIterator + 1) % MAX_HW_QUERIES)
                {
                    if (m_CorrespondingQueryEnds[queryIterator] != Int32.MaxValue)
                    {
                        var profilerObject = new ProfilerTreeMember();
                        int correspondingEnd = m_CorrespondingQueryEnds[queryIterator];
                        long beginProfilePointData = context.GetData<long>(m_HWQueries[queryIterator]);
                        long endProfilePointData = context.GetData<long>(m_HWQueries[correspondingEnd]);

                        profilerObject.m_Time = (double)(endProfilePointData - beginProfilePointData) / (double)disjointData.Frequency * 1000.0;
                        profilerObject.m_Name = m_HWQueriesDescs[queryIterator];
                        profilerObject.m_Parent = parent;

                        parent.m_ChildMembers.Add(profilerObject);
                        parent = profilerObject;
                    }
                    else
                    {
                        parent = parent.m_Parent;
                        if (parent == null)
                        {
                            throw new Exception("Error while constructing profiler tree");
                        }
                    }
                }
                if (frameParent.m_ChildMembers.Count < 1)
                {
                    throw new Exception("Error while constructing profiler tree");
                }

                m_CurrentFrameProfilerTree = frameParent.m_ChildMembers[0];
            }
        }
    }

    class GpuProfilePoint : IDisposable
    {
        DeviceContext context;

        public GpuProfilePoint(DeviceContext context, String name)
        {
            this.context = context;
            GPUProfiler.BeginProfilePoint(context, name);
        }


        public void Dispose()
        {
            GPUProfiler.EndProfilePoint(context);
        }
    }
}
