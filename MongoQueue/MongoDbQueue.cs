﻿using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskQueue;
using TaskQueue.Providers;
using TaskUniversum;

namespace MongoQueue
{
    public class MongoDbQueue : ITQueue
    {
        const int TupleSize = 100;

        MongoCollection<MongoQueue.MongoMessage> Collection;
        IMongoSortBy SortFeature;
        IMongoQuery QueryFeature;

        TQItemSelector selector;

        public void OptimiseForSelector()
        {
            CheckConnection();

            try
            {
                Collection.EnsureIndex(MongoSelector.GetIndex(selector));
            }
            catch (MongoConnectionException e)
            {
                throw new QueueConnectionException("can't ensure index", e);
            }
        }

        public void SetSelector(TQItemSelector selector)
        {
            this.selector = selector;
            this.QueryFeature = MongoSelector.GetQuery(this.selector);
            this.SortFeature = MongoSelector.GetSort(this.selector);
            OptimiseForSelector();
        }

        public long GetQueueLength()
        {
            var cursor = Collection.Find(QueryFeature);
            long result = cursor.Count();
            return result;
        }

        public void Push(TaskMessage item)
        {
            CheckConnection();

            //WriteConcernResult result = Collection.Insert(new BsonDocument(item.GetHolder()), new MongoInsertOptions() { WriteConcern = new WriteConcern() { Journal = true } });
            //WriteConcernResult result = Collection.Insert(new MongoMessage { ExtraElements = item.GetHolder() }, new MongoInsertOptions() { WriteConcern = new WriteConcern() { Journal = true } });
            WriteConcernResult result = Collection.Insert(new MongoMessage { ExtraElements = item.GetHolder() });
            // TODO: QueueOverflowException
            
            if (!result.Ok)
                throw new Exception("error in push to mongo queue: " + result.ToJson());
        }
        public T GetItemFifo<T>()
            where T : TaskMessage
        {
            TaskMessage tmr = GetItemFifo();
            T tmo = Activator.CreateInstance<T>();
            tmo.SetHolder(tmr.Holder);
            return tmo;
        }
        public TaskMessage GetItemFifo()
        {
            CheckConnection();

            TaskQueue.TQItemSelector selector = TaskQueue.TQItemSelector.DefaultFifoSelector;
            var cursor = Collection.Find(MongoSelector.GetQuery(selector)).SetSortOrder();
            cursor.Limit = 1;
            MongoMessage mms = cursor.FirstOrDefault();
            if (mms == null)//empty
                return null;
            TaskMessage msg = new TaskMessage(mms.ExtraElements);
            msg.Holder.Add("_id", mms.id.Value);
            return msg;
        }

        public TaskMessage GetItem()
        {
            CheckConnection();

            var cursor = Collection.Find(QueryFeature).SetSortOrder(SortFeature);
            cursor.Limit = 1;
            MongoMessage mms = cursor.FirstOrDefault();
            if (mms == null)//empty
                return null;
            TaskMessage msg = new TaskMessage(mms.ExtraElements);
            msg.Holder.Add("_id", mms.id.Value);
            return msg;
        }
        public void UpdateItem(TaskMessage item)
        {
            Dictionary<string, object> holder = item.GetHolder();
            object id = holder["_id"];
            if (id == null || !(id is ObjectId))
                throw new Exception("_id of queue element is missing");

            BsonObjectId objid = new BsonObjectId((ObjectId)id);
            holder.Remove("_id");

            MongoMessage msg = new MongoMessage
            {
                ExtraElements = holder,
                id = objid
            };

            //var result = Collection.Save(msg, new MongoInsertOptions() { WriteConcern = new WriteConcern() { Journal = true } });
            var result = Collection.Save(msg);
            if (!result.Ok)
                throw new Exception("error in update to mongo queue: " + result.ToJson());
        }

        public void InitialiseFromModel(RepresentedModel model, QueueConnectionParameters connection)
        {
            this.model = model;
            this.connection = connection;

            SetSelector(TQItemSelector.DefaultFifoSelector);
            try
            {
                OpenConnection(connection);
            }
            catch (Exception e)
            {
                throw new QueueConnectionException("can't open connection to: " + connection.ConnectionString, e);
            }
        }

        private void OpenConnection(QueueConnectionParameters connection)
        {
            MongoClient cli = new MongoClient(connection.ConnectionString);
            var server = cli.GetServer();
            var db = server.GetDatabase(connection.Database);
            Collection = db.GetCollection<MongoQueue.MongoMessage>(connection.Collection);
            Connected = true;
        }

        private void CheckConnection()
        {
            if (!Connected)
            {
                OpenConnection(this.connection);
            }
        }

        public string QueueType
        {
            get { return "MongoDBQ"; }
        }

        public string QueueDescription
        {
            get { return "MongoDB queue"; }
        }

        public TaskMessage[] GetItemTuple()
        {
            var cursor = Collection.Find(QueryFeature).SetSortOrder(SortFeature).SetBatchSize(TupleSize);
            cursor.Limit = TupleSize;
            List<TaskMessage> tma = new List<TaskMessage>();
            foreach (var mms in cursor)
            {
                TaskMessage msg = new TaskMessage(mms.ExtraElements);
                msg.Holder.Add("_id", mms.id.Value);
                tma.Add(msg);
            }

            return tma.ToArray();
        }

        public void Purge()
        {
            CheckConnection();
            Collection.RemoveAll();
        }

        public bool Connected { get; set; }
        RepresentedModel model { get; set; }
        QueueConnectionParameters connection { get; set; }



        public QueueSpecificConnectionParameters GetParametersModel()
        {
            throw new NotImplementedException();
        }
    }
}
