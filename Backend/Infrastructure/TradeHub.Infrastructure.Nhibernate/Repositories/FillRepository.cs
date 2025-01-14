/***************************************************************************** 
* Copyright 2016 Aurora Solutions 
* 
*    http://www.aurorasolutions.io 
* 
* Aurora Solutions is an innovative services and product company at 
* the forefront of the software industry, with processes and practices 
* involving Domain Driven Design(DDD), Agile methodologies to build 
* scalable, secure, reliable and high performance products.
* 
* TradeSharp is a C# based data feed and broker neutral Algorithmic 
* Trading Platform that lets trading firms or individuals automate 
* any rules based trading strategies in stocks, forex and ETFs. 
* TradeSharp allows users to connect to providers like Tradier Brokerage, 
* IQFeed, FXCM, Blackwood, Forexware, Integral, HotSpot, Currenex, 
* Interactive Brokers and more. 
* Key features: Place and Manage Orders, Risk Management, 
* Generate Customized Reports etc 
* 
* Licensed under the Apache License, Version 2.0 (the "License"); 
* you may not use this file except in compliance with the License. 
* You may obtain a copy of the License at 
* 
*    http://www.apache.org/licenses/LICENSE-2.0 
* 
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, 
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
* See the License for the specific language governing permissions and 
* limitations under the License. 
*****************************************************************************/


﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Stereotype;
using Spring.Transaction;
using Spring.Transaction.Interceptor;
using TradeHub.Common.Core.DomainModels.OrderDomain;
using TradeHub.Common.Core.Repositories;
using TradeHub.Common.Core.Repositories.Parameters;

namespace TradeHub.Infrastructure.Nhibernate.Repositories
{
    /// <summary>
    /// Execution fill repository
    /// </summary>
    [Repository]
    public class FillRepository:HibernateDao,IFillRepository
    {
        /// <summary>
        /// DB Table in which the Order Information is persisted
        /// Only used during custom SQL Query fetch
        /// </summary>
        private const string Table = "fill";

        /// <summary>
        /// Add or update existing Fill
        /// </summary>
        /// <param name="entity"></param>
        [Transaction(TransactionPropagation.Required, ReadOnly = false)]
        public void AddUpdate(Fill entity)
        {
            CurrentSession.SaveOrUpdate(entity);
        }

        /// <summary>
        /// Add update collection
        /// </summary>
        /// <param name="collection"></param>
        [Transaction(TransactionPropagation.Required, ReadOnly = false)]
        public void AddUpdate(IEnumerable<Fill> collection)
        {
            foreach (var entity in collection)
            {
                CurrentSession.SaveOrUpdate(entity);
            }
        }

        /// <summary>
        /// Delete Fill entity
        /// </summary>
        /// <param name="entity"></param>
        [Transaction(TransactionPropagation.Required, ReadOnly = false)]
        public void Delete(Fill entity)
        {
            CurrentSession.Delete(entity);
        }

        /// <summary>
        /// List all Fills
        /// </summary>
        /// <returns></returns>
        [Transaction(ReadOnly = true)]
        public IList<Fill> ListAll()
        {
            IList<Fill> list = null;
            list = CurrentSession.CreateCriteria<Fill>()
                   .List<Fill>();
            return list;
        }

        /// <summary>
        /// Find fill by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Transaction(ReadOnly = true)]
        public Fill FindBy(string id)
        {
            return CurrentSession.Get<Fill>(id);
        }

        /// <summary>
        /// List all orders
        /// </summary>
        /// <returns></returns>
        [Transaction(ReadOnly = true)]
        public IList<object> Find(Dictionary<FillParameters, string> parameters)
        {
            // Create Valid WHERE Clause for incoming request
            string whereClause = CreateWhereClause(parameters);

            // Incoming Clause should not be empty
            if (!string.IsNullOrEmpty(whereClause))
            {
                // Create SQL Query
                string sqlQuery = "SELECT * FROM " + Table + whereClause;

                // Fetch Information from DB
                var result = CurrentSession.CreateSQLQuery(sqlQuery).List<object>();

                // Return Information
                return result;
            }

            return null;
        }

        /// <summary>
        /// Create Where Clause to be used in SQL query from the incoming parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string CreateWhereClause(Dictionary<FillParameters, string> parameters)
        {
            string whereClause = string.Empty;
            string joinStatement = string.Empty;

            bool initialValue = true;
            OrderParameters joinFlag = OrderParameters.ExecutionId;

            // Traverse Parameters
            foreach (KeyValuePair<FillParameters, string> valuePair in parameters)
            {
                // Check if JOIN is to be created
                if (valuePair.Key.HasFlag(joinFlag))
                {
                    joinStatement = CreateJoinSegment("fill", OrderParameters.OrderId.ToString());
                }

                if (initialValue)
                {
                    initialValue = false;

                    whereClause += " WHERE ";
                }
                else
                {
                    whereClause += " AND ";
                }

                // Check if 'OR' condition needs to be placed
                if (valuePair.Value.Contains(','))
                {
                    // Split on ',' to separate values as individual
                    string[] splitValues = valuePair.Value.Split(',');

                    // Save length to be used in further processing
                    int count = splitValues.Length;

                    for (int i = 0; i < count; i++)
                    {
                        // Start 'OR' Comparison
                        if (i == 0)
                        {
                            whereClause += " ( ";
                        }

                        // Create Comparison
                        whereClause += valuePair.Key + " = '" + splitValues[i] + "'";

                        // Close 'OR' Comparison
                        if (i == (count - 1))
                        {
                            whereClause += " )";
                        }
                        // Place 'OR' Condition
                        else
                        {
                            whereClause += " OR ";
                        }
                    }
                }
                // Place 'AND' Condition
                else
                {
                    // Check for Start Date Time Value
                    if (valuePair.Key.Equals(OrderParameters.StartOrderDateTime))
                    {
                        whereClause += OrderParameters.OrderDateTime + " >= '" + valuePair.Value + "'";
                    }
                    // Check for End Date Time Value
                    else if (valuePair.Key.Equals(OrderParameters.EndOrderDateTime))
                    {
                        {
                            whereClause += OrderParameters.OrderDateTime + " <= '" + valuePair.Value + "'";
                        }
                    }
                    else
                    {
                        whereClause += valuePair.Key + " = '" + valuePair.Value + "'";
                    }
                }
            }

            return joinStatement + whereClause;
        }

        /// <summary>
        /// Creates JOIN segment to be used in the SQL Query
        /// </summary>
        /// <param name="joinTable">Second Table in the JOIN statement</param>
        /// <param name="property">JOIN Property</param>
        /// <returns></returns>
        private string CreateJoinSegment(string joinTable, string property)
        {
            return " INNER JOIN " + joinTable + " ON " + Table + "." + property + "=" + joinTable + "." + property;
        }
    }
}
