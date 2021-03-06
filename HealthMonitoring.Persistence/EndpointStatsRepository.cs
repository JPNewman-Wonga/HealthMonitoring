﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using HealthMonitoring.Management.Core.Repositories;
using HealthMonitoring.Model;
using HealthMonitoring.Persistence.Entities;

namespace HealthMonitoring.Persistence
{
    public class EndpointStatsRepository : IEndpointStatsRepository
    {
        private readonly MySqlDatabase _db;

        public EndpointStatsRepository(MySqlDatabase db)
        {
            _db = db;
        }

        public void InsertEndpointStatistics(Guid endpointId, EndpointHealth stats)
        {
            using (var conn = _db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                var id = Guid.NewGuid();
                conn.Execute(
                    "insert into EndpointStats (Id, EndpointId, CheckTimeUtc, ResponseTime, Status) values (@id, @endpointId, @checkTimeUtc, @responseTime, @status)",
                    new { id, endpointId, stats.CheckTimeUtc, responseTime = stats.ResponseTime.Ticks, stats.Status }, tx);
                tx.Commit();
            }
        }

        public IEnumerable<EndpointStats> GetStatistics(Guid id, int limitDays)
        {
            using (var conn = _db.OpenConnection())
            {
                return conn
                    .Query<EndpointStatsEntity>("select CheckTimeUtc, ResponseTime, Status from EndpointStats where endpointId=@id and checkTimeUtc > @dateLimit order by checkTimeUtc", new { id, dateLimit = DateTime.UtcNow.AddDays(-limitDays) })
                    .Select(e => e.ToDomain());
            }
        }

        public int DeleteStatisticsOlderThan(DateTime date, int maxBatchSize)
        {
            using (var conn = _db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                var count = conn.Execute("delete from EndpointStats where CheckTimeUtc < @date order by CheckTimeUtc limit @maxBatchSize", new { date, maxBatchSize }, tx);
                tx.Commit();
                return count;
            }
        }
    }
}