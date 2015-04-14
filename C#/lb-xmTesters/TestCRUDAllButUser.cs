using System;
using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;

namespace UnitTests
{
	[TestFixture]
	public class TestCRUDAllButUser
	{
		#region params
		IList<Customer> customerList;
		IList<Review> reviewList;
		IList<Order> orderList;
		int initCustomerCount = 5 , initReviewCount = 5, initOrderCount = 5;
		#endregion


		#region testfixture setup/teardown
		[TestFixtureSetUp]
		public  void Setup(){
			Gateway.SetServerBaseURLToSelf ();
			customerList = Customers.Find ().Result;
			reviewList = Reviews.Find ().Result;
			orderList = Orders.Find ().Result;

		}
		#endregion

		//CRUD Tests

		//Customers
		#region Customers CRUD
		//Customer Count test
		[Test]
		public void CustomerCount(){
			Assert.AreEqual (initCustomerCount, Customers.Count ().Result);
		}

		//Customer Exists test
		[Test]
		public void CustomerExists(){
			Assert.AreEqual(true, Customers.Exists (customerList[0].id).Result);
			Assert.AreEqual(true, Customers.Exists (customerList[1].id).Result);
			Assert.AreEqual(false, Customers.Exists ("-5").Result);
			Assert.AreEqual(false, Customers.Exists 
				((Convert.ToInt32(customerList[4].id) + 20).ToString()).Result);
			Assert.AreEqual(false, Customers.Exists 
				((Convert.ToInt32(customerList[4].id) + 40).ToString()).Result);
		}

		//Customer FindById
		[Test]
		public void CustomerFindById(){
			Customer real = customerList [3];
			Customer foundCustomer = Customers.FindById (real.id).Result;
			Assert.AreNotEqual (null, foundCustomer);
			Assert.AreEqual (real.name, foundCustomer.name);
			Assert.AreEqual (real.id, foundCustomer.getID());
			Assert.AreEqual (real.age, foundCustomer.age);
			try
			{
				foundCustomer = Customers.FindById ("-5").Result;
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				var restException = (RestException)e.InnerException;
				Assert.AreEqual(404, restException.StatusCode);
			}

		}

		//Customer Find
		[Test]
		public void CustomerFind(){
			IList<Customer> CustList = Customers.Find ().Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (Customers.Count().Result, CustList.Count);
			int i = 1;
			foreach (Customer cust in CustList){
				string str = i.ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}
		}

		//Customer Find with filter
		[Test]
		public void CustomerFindWithFilter(){
			const string badFilter = "{\"where\": {\"id\": \"1000\"}}";
			const string filter = "{\"where\": {\"and\": [{\"age\": {\"gt\": 22}} ,{\"age\": {\"lt\": 25}}]}}";
			IList<Customer> CustList = Customers.Find (filter).Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (2, CustList.Count);
			int[] arr = { 3, 4 };
			int i = 0;
			foreach (Customer cust in CustList){
				string str = arr[i].ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}

			CustList = Customers.Find (badFilter).Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (0, CustList.Count); 
		}

		//Customer FindOne
		[Test]
		public void CustomerFindOne(){
			const string badFilter = "{\"where\": {\"id\": \"1000\"}}";
			const string filter = "{\"where\": {\"and\": [{\"age\": {\"gt\": 22}} ,{\"age\": {\"lt\": 25}}]}}";
			try
			{
				Customer result = Customers.FindOne(badFilter).Result;
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				var restException = (RestException)e.InnerException;
				Assert.AreEqual(404, restException.StatusCode);
			}

			Customer cust = Customers.FindOne ().Result;
			Assert.AreNotEqual (null, cust);
			Assert.AreEqual (customerList[0].id, cust.getID ());

			cust = CRUDInterface<Customer>.FindOne (filter).Result;
			Assert.AreNotEqual (null, cust);
			Assert.AreEqual (customerList[2].id, cust.getID ());
		}

		//Customer UpdateById
		[Test]
		public void CustomerUpdateById(){
			string id = customerList [2].id;
			var updateData = new Customer {
				name = "sdfsd"
			};
			Customer beforeUpdate = Customers.FindById (id).Result;
			Customer upResult = Customers.UpdateById (id, updateData).Result;

			Assert.AreNotEqual (null, upResult);
			Assert.AreEqual (id, upResult.getID ());
			Assert.AreEqual(updateData.name, upResult.name);
			Assert.AreNotEqual (0, upResult.age);
			//update back
			Customers.UpdateById (id, beforeUpdate).Wait ();

		}

		//Customer UpdateAll
		[Test]
		public void CustomerUpdateAll(){
			const string whereFilter = "{\"and\": [{\"age\": {\"gte\": 30}} ,{\"age\": {\"lt\": 34}}]}";
			const string filter = "{\"where\": {\"and\": [{\"age\": {\"gte\": 30}} ,{\"age\": {\"lt\": 34}}]}}";
			var updateData = new Customer {
				name = "Jimbob"
			};
			var newIds = new List<string> ();
			for (int i = 0; i < 4; i++) {
				var newCust = new Customer {
					age = 30 + i,
					name = "Customer For updateAll " + i
				};
				newCust = Customers.Create (newCust).Result;
				newIds.Add (newCust.id);
			}

			Customers.UpdateAll (updateData, whereFilter).Wait ();
			IList<Customer> CustList = Customers.Find (filter).Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (4, CustList.Count);
			foreach (Customer cust in CustList){
				Assert.AreEqual(updateData.name, cust.name);
				Assert.AreNotEqual (updateData.age, cust.age);
			}
			foreach (string id in newIds) {
				Customers.DeleteById (id).Wait();
			}
		}

		//Customer create and delete
		[Test]
		public void CustomerCreateAndDelete(){
			var newCustomer = new Customer {
				age = 121,
				name = "testCreate"
			};
			Customer newCustomerResp = Customers.Create(newCustomer).Result;
			Assert.AreNotEqual (null, newCustomerResp);
			Assert.AreEqual (newCustomer.age, newCustomerResp.age);
			Assert.AreEqual (newCustomer.name, newCustomerResp.name);
			Assert.AreEqual (initCustomerCount + 1, Customers.Count ().Result);
			Customers.DeleteById (newCustomerResp.id).Wait();
			Assert.AreEqual (false, Customers.Exists (newCustomerResp.id).Result);
			Assert.AreEqual (initCustomerCount, Customers.Count ().Result);
		}


		//Customer upsert
		[Test]
		public void CustomerUpsert(){
			var newCustomer = new Customer {
				age = 121,
				name = "testUpsert"
			};
			//Upsert new customer
			Customer upsertResult = Customers.Upsert (newCustomer).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newCustomer.age, upsertResult.age);
			Assert.AreEqual (newCustomer.name, upsertResult.name);
			Assert.AreEqual (initCustomerCount + 1, Customers.Count ().Result);
			//Upsert update customer
			string newId = upsertResult.getID ();
			var upCustomer = new Customer {
				age = 150,
				name = "Jimmy",
				id = newId
			};
			upsertResult = Customers.Upsert (upCustomer).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (upCustomer.age, upsertResult.age);
			Assert.AreEqual (upCustomer.name, upsertResult.name);
			Customers.DeleteById (newId).Wait();

		}
		#endregion
		//Reviews

		#region Reviews
		//Reviews Count test
		[Test]
		public void ReviewsCount(){
			Assert.AreEqual (initReviewCount, Reviews.Count ().Result);
		}

		//Reviews Exists test
		[Test]
		public void ReviewsExists(){
			Assert.AreEqual(true, Reviews.Exists (reviewList[2].id).Result);
			Assert.AreEqual(true, Reviews.Exists (reviewList[3].id).Result);
			Assert.AreEqual(true, Reviews.Exists (reviewList[0].id).Result);
			Assert.AreEqual(false, Reviews.Exists ("-5").Result);
			Assert.AreEqual(false, Reviews.Exists 
				((Convert.ToInt32(reviewList[4].id) + 20).ToString()).Result);
			Assert.AreEqual(false, Reviews.Exists 
				((Convert.ToInt32(reviewList[4].id) + 50).ToString()).Result);
		}

		//Reviews FindById
		[Test]
		public void ReviewsFindById(){
			Review real = reviewList [3];
			Review foundReview = Reviews.FindById (real.id).Result;
			Assert.AreNotEqual (null, foundReview);
			Assert.AreEqual (real.product, foundReview.product);
			Assert.AreEqual (real.star, foundReview.star);
			Assert.AreEqual (real.authorId, foundReview.authorId);
			Assert.AreEqual (real.id, foundReview.id);
			try
			{
				foundReview = Reviews.FindById ("-5").Result;
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				var restException = (RestException)e.InnerException;
				Assert.AreEqual(404, restException.StatusCode);
			}
		}

		//Reviews Find
		[Test]
		public void ReviewsFind(){
			IList<Review> CustList = Reviews.Find ().Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (Reviews.Count().Result, CustList.Count);
			int i = 1;
			foreach (Review rev in CustList){
				string str = i.ToString();
				Assert.AreEqual(str, rev.getID());
				++i;
			}
		}

		//Reviews Find with filter
		[Test]
		public void ReviewsFindWithFilter(){
			const string badFilter = "{\"where\": {\"id\": \"1000\"}}";
			const string filter = "{\"where\": {\"star\": {\"lte\": 4}} }";
			IList<Review> RevList = Reviews.Find (filter).Result;
			Assert.AreNotEqual (null, RevList);
			Assert.AreEqual (3, RevList.Count);
			int[] arr = { 1, 2, 4 };
			int i = 0;
			foreach (Review cust in RevList){
				string str = arr[i].ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}

			RevList = Reviews.Find (badFilter).Result;
			Assert.AreNotEqual (null, RevList);
			Assert.AreEqual (0, RevList.Count); 
		}

		//Reviews FindOne
		[Test]
		public void ReviewsFindOne(){
			const string badFilter = "{\"where\": {\"id\": \"1000\"}}";
			const string filter = "{\"where\": {\"star\": {\"gte\": 4}} }";
			try
			{
				Review result = Reviews.FindOne(badFilter).Result;
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				var restException = (RestException)e.InnerException;
				Assert.AreEqual(404, restException.StatusCode);
			}
			Review rev = Reviews.FindOne ().Result;
			Assert.AreNotEqual (null, rev);
			Assert.AreEqual (reviewList[0].id, rev.getID ());

			rev = Reviews.FindOne (filter).Result;
			Assert.AreNotEqual (null, rev);
			Assert.AreEqual (reviewList[1].id, rev.getID ());
		}

		//Reviews UpdateById
		[Test]
		public void ReviewsUpdateById(){
			string id = reviewList [0].id;
			var updateData = new Review {
				product = "productXXX",
				authorId = 2,
				star = 1
			};
			Review beforeUpdate = Reviews.FindById (id).Result;
			Review upResult = Reviews.UpdateById (id, updateData).Result;
			Assert.AreNotEqual (null, upResult);
			Assert.AreEqual (id, upResult.getID ());
			Assert.AreEqual(updateData.product, upResult.product);
			Assert.AreEqual (updateData.star, upResult.star);
			Assert.AreEqual (updateData.authorId, upResult.authorId);
			//update back
			Reviews.UpdateById (id, beforeUpdate).Wait();
		}

		//Reviews UpdateAll
		[Test]
		public void ReviewsUpdateAll(){
			var updateData = new Review  {
				star = 1
			};
			var newIds = new List<string> ();
			for (int i = 0; i < 4; i++) {
				var newRev = new Review  {
					product = "newProduct" + i,
					star = i, 
					authorId = 70 + i
				};
				newRev = Reviews.Create (newRev).Result;
				newIds.Add (newRev.id);
			}
			Reviews.UpdateAll (updateData, "{\"authorId\": {\"gte\": 70}}").Wait();
			IList<Review> RevList = Reviews.Find ("{\"where\": {\"authorId\": {\"gte\": 70}} }").Result;
			Assert.AreNotEqual (null, RevList);
			Assert.AreEqual (4, RevList.Count);
			foreach (Review rev in RevList){
				Assert.AreEqual(updateData.star, rev.star);
			}
			foreach(string id in newIds){
				Reviews.DeleteById(id).Wait();
			}
		}

		//Reviews create and delete
		[Test]
		public void ReviewsCreateAndDelete(){
			var newReview = new Review {
				product = "newProduct",
				star = 3, 
				authorId = 1
			};
			Review newRevResp = Reviews.Create(newReview).Result;
			Assert.AreNotEqual (null, newRevResp);
			Assert.AreEqual (newReview.product, newRevResp.product);
			Assert.AreEqual (newReview.star, newRevResp.star);
			Assert.AreEqual (newReview.authorId, newRevResp.authorId);
			Assert.AreEqual (initReviewCount + 1, Reviews.Count ().Result);
			Reviews.DeleteById (newRevResp.id).Wait();
			Assert.AreEqual (false, Reviews.Exists (newRevResp.id).Result);
			Assert.AreEqual (initReviewCount, Reviews.Count ().Result);
		}

		//Reviews upsert
		[Test]
		public void ReviewsUpsert(){
			var newReview = new Review {
				product = "newProduct",
				star = 3, 
				authorId = 1
			};
			//Upsert new Reviews
			Review upsertResult = Reviews.Upsert (newReview).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newReview.product, upsertResult.product);
			Assert.AreEqual (newReview.star, upsertResult.star);
			Assert.AreEqual (newReview.authorId, upsertResult.authorId);
			Assert.AreEqual (initReviewCount + 1, Reviews.Count ().Result);

			//Upsert update Reviews
			string newId = upsertResult.getID ();
			var upReview = new Review {
				product = "productXXY1",
				star = 5, 
				authorId = 4,
				id = newId
			};
			upsertResult = Reviews.Upsert (upReview).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (upReview.product, upsertResult.product);
			Assert.AreEqual (upReview.star, upsertResult.star);
			Assert.AreEqual (upReview.authorId, upsertResult.authorId);
			Reviews.DeleteById (upsertResult.id).Wait();

		}
		#endregion

		//Orders
		#region Orders
		//Orders Count test
		[Test]
		public void OrdersCount(){
			Assert.AreEqual (initOrderCount, Orders.Count ().Result);
		}

		//Orders Exists test
		[Test]
		public void OrdersExists(){
			Assert.AreEqual(true, Orders.Exists (orderList[0].id).Result);
			Assert.AreEqual(true, Orders.Exists (orderList[2].id).Result);
			Assert.AreEqual(true, Orders.Exists (orderList[3].id).Result);
			Assert.AreEqual(false, Orders.Exists ((Convert.ToInt32(orderList[4].id) + 20).ToString()).Result);
			Assert.AreEqual(false, Orders.Exists ("-5").Result);
			Assert.AreEqual(false, Orders.Exists ((Convert.ToInt32(orderList[4].id) + 50).ToString()).Result);
		}

		//Orders FindById
		[Test]
		public void OrdersFindById(){
			Order real = orderList [3];
			Order foundOrder = Orders.FindById (real.id).Result;
			Assert.AreNotEqual (null, foundOrder);
			Assert.AreEqual (real.description, foundOrder.description);
			Assert.AreEqual (real.total, foundOrder.total);
			Assert.AreEqual (real.customerId, foundOrder.customerId);
			Assert.AreEqual (real.id, foundOrder.id);
			try
			{
				foundOrder = Orders.FindById ("-6").Result;
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				var restException = (RestException)e.InnerException;
				Assert.AreEqual(404, restException.StatusCode);
			}
		}

		[Test]
		//Orders Find
		public void OrdersFind(){
			IList<Order> OrdList = Orders.Find ().Result;
			Assert.AreNotEqual (null, OrdList);
			Assert.AreEqual (Orders.Count().Result, OrdList.Count);
			int i = 1;
			foreach (Order ord in OrdList){
				string str = i.ToString();
				Assert.AreEqual(str, ord.getID());
				++i;
			}
		}

		//Orders Find with filter
		[Test]
		public void OrdersFindWithFilter(){
			const string badFilter = "{\"where\": {\"id\": \"1000\"}}";
			const string filter = "{\"where\": {\"total\": {\"lte\": 200}} }";
			IList<Order> OrdList = Orders.Find (filter).Result;
			Assert.AreNotEqual (null, OrdList);
			Assert.AreEqual (3, OrdList.Count);
			int[] arr = { 2, 4, 5 };
			int i = 0;
			foreach (Order cust in OrdList){
				string str = arr[i].ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}

			OrdList = Orders.Find (badFilter).Result;
			Assert.AreNotEqual (null, OrdList);
			Assert.AreEqual (0, OrdList.Count); 
		}

		//Orders FindOne
		[Test]
		public void OrdersFindOne(){
			const string badFilter = "{\"where\": {\"id\": \"1000\"}}";
			const string filter = "{\"where\": {\"total\": {\"lte\": 200}} }";
			Order result;
			try
			{
				result = Orders.FindOne(badFilter).Result;
				Assert.Fail();
			}
			catch (AggregateException e)
			{
				var restException = (RestException)e.InnerException;
				Assert.AreEqual(404, restException.StatusCode);
			}
			result = Orders.FindOne ().Result;
			Assert.AreNotEqual (null, result);
			Assert.AreEqual (orderList[0].id, result.getID ());
			result =Orders.FindOne (filter).Result;
			Assert.AreNotEqual (null, result);
			Assert.AreEqual (orderList[1].id, result.getID ());
		}

		//Orders UpdateById
		[Test]
		public void OrdersUpdateById(){
			string id = orderList [4].id;
			var updateData = new Order {
				description = "OrderXX",
				customerId = 4,
				total = 20
			};
			Order beforeUpdate = Orders.FindById (id).Result;
			Order upResult = Orders.UpdateById (id, updateData).Result;
			Assert.AreNotEqual (null, upResult);
			Assert.AreEqual (id, upResult.getID ());
			Assert.AreEqual(updateData.description, upResult.description);
			Assert.AreEqual (updateData.customerId, upResult.customerId);
			Assert.AreEqual (updateData.total, upResult.total);
			//update back
			Orders.UpdateById (id, beforeUpdate).Wait();
		}

		//Orders UpdateAll
		[Test]
		public void OrdersUpdateAll(){
			const string filter = "{\"where\": {\"customerId\": {\"gte\": 70}} }";
			const string whereFilter = "{\"customerId\": {\"gte\": 70}}";
			var updateData = new Order {
				total = 100
			};
			var newIds = new List<string> ();
			for (int i = 0; i < 4; i++) {
				var newOrd = new Order {
					description = "OrderXX" + i,
					customerId = 70 + i,
					total = 220
				};
				newOrd = Orders.Create (newOrd).Result;
				newIds.Add (newOrd.id);
			}
			Orders.UpdateAll (updateData, whereFilter).Wait();
			IList<Order> ordList = Orders.Find (filter).Result;
			Assert.AreNotEqual (null, ordList);
			Assert.AreEqual (4, ordList.Count);
			foreach (Order ord in ordList){
				Assert.AreEqual(updateData.total, ord.total);
			}
			foreach (string id in newIds) {
				Orders.DeleteById (id).Wait ();
			}
		}

		//Orders create
		[Test]
		public void OrdersCreateAndDelete(){
			var newOrder = new Order {
				description = "OrderXXX",
				customerId = 4,
				total = 300.5
			};
			Order newOrdResp = Orders.Create(newOrder).Result;
			Assert.AreNotEqual (null, newOrdResp);
			Assert.AreEqual (newOrder.description, newOrdResp.description);
			Assert.AreEqual (newOrder.customerId, newOrdResp.customerId);
			Assert.AreEqual (newOrder.total, newOrdResp.total);
			Assert.AreEqual (initOrderCount + 1, Orders.Count ().Result);
			Orders.DeleteById (newOrdResp.id).Wait ();
			Assert.AreEqual (false, Orders.Exists (newOrdResp.id).Result);
			Assert.AreEqual (initOrderCount, Orders.Count ().Result);
		}

		//Orders upsert
		[Test]
		public void OrdersUpsert(){
			var newOrder = new Order {
				description = "OrderXXX",
				customerId = 4,
				total = 300.5
			};
			//Upsert new Orders
			Order upsertResult = Orders.Upsert (newOrder).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newOrder.description, upsertResult.description);
			Assert.AreEqual (newOrder.customerId, upsertResult.customerId);
			Assert.AreEqual (newOrder.total, upsertResult.total);
			Assert.AreEqual (initOrderCount + 1, Orders.Count ().Result);

			//Upsert update Reviews
			string newId = upsertResult.getID ();
			newOrder = new Order {
				description = "OrderXXXXX",
				customerId = 4,
				total = 100.5,
				id = newId
			};
			upsertResult = Orders.Upsert (newOrder).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newOrder.description, upsertResult.description);
			Assert.AreEqual (newOrder.customerId, upsertResult.customerId);
			Assert.AreEqual (newOrder.total, upsertResult.total);
			Orders.DeleteById (newId).Wait();
		}
		#endregion



	}
}

