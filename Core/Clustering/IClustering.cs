using System;
using System.Collections.Generic;
using Colorissimo.Core;

namespace Colorissimo.Core.Clustering
{

    public interface IClustering
    {
        /// <summary>
        /// Set index points for clustering
        /// </summary>
        /// <param name="list"></param>
        void SetPoints(Color3[] list);

        /// <summary>
        /// Add cluster to list
        /// </summary>
        /// <param name="cluster"></param>
        void AddCluster(Color3 cluster);

        /// <summary>
        /// Start clustering
        /// </summary>
        void Start();

        /// <summary>
        /// Run clustering
        /// </summary>
        void Run();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Color3[] Clusters(int count);
    }

}