using System;
using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;

namespace UnitTests
{
	/*
	 * e_TestRelations class
	 * Tests relations between the Customer model and Order + Reviews
	 * Customer has many orders and reviews
	 * Orders and Review belong to Customer
	 * 
	 * The tester builds an automatic scenerion with a given input:
	 * 3 customers - each has a given amount of orders and review (min 3 each)
	 * runs tests on all the ralation function that are on the server.
	 * 
	 * input:
	 * ords - array of order amounts for each customer
	 * revs - array of review amounts for each customer
	 * Expected number of orders and reviews is >= 3 for each. otherwise tes will fail.
	 * */
	[TestFixture(new int[] { 4, 4, 4 },new int[] { 4,4, 4 })]
	public class TestRelations{
		/*Constructor
		 * input: 
		 * 	ords - array of orders per Customer
		 * 	revs - array of reviews per customer
		 * output:
		 * 	new test
		 * */
		public TestRelations(int[] ords = null, int[] revs = null)
		{
			this.ords = ords != null && ords.Length == 3 ? ords : new int[]{ 3, 3, 3 };
			this.revs = revs != null && revs.Length == 3 ? revs : new int[]{ 3, 3, 3 };

		}
		#region params
		IList<Order> OldOrdList;
		IList<Review> OldRevList;
		IList<Customer> OldCustList;
		int[] ords, revs;
		IList<Order> OrdList = new List<Order>();
		IList<Review> RevList = new List<Review>();
		IList<Customer> CustList = new List<Customer>();
		string custToDestroyIn = ""; //id of the customer that deletes one of each items
		string custToDeletein = ""; //id of the customer that deletes all items
		string orderToDelete = ""; //order id to delet in custToDestroy
		string reviewToDelete = ""; //review to destroy in custToDestroy
		string reviewToFind = ""; 
		string orderToFind = "";
		string custForOuterRelations = "";  //id of customer to test the belons to relation
		string orderRelationsDestroy = ""; //order to delete for custForOuterRelations
		string reviewRelationsDestroy = "";
		string orderToGetFor = "";
		string reviewToGetFor = "";
		#endregion
		#region fuctions
		/*deleteAllCustomers
		 * deletes all customers on server
		 * */
		public void deleteAllCustomers(){
			IList<Customer> CustList = Customers.Find ("").Result;
			foreach (Customer cust in CustList){
				Customers.DeleteById(cust.getID()).Wait();
			}
		}

		/*deleteAllReviews
		 * deletes all reviews on server
		 * */
		public void deleteAllReviews(){
			IList<Review> RevList = Reviews.Find ("").Result;
			foreach (Review rev in RevList){
				Reviews.DeleteById(rev.getID()).Wait();
			}
		}

		/*deleteAllOrders
		 * deletes all orders on server
		 * */
		public void deleteAllOrders(){
			IList<Order> OrdList = Orders.Find ("").Result;
			foreach (Order ord in OrdList){
				Orders.DeleteById (ord.getID ()).Wait ();
			}
		}

		/*addOrder
		 * adds an order to server and saves it to local list
		 * */
		public void addOrder(Order ord){
			OrdList.Add(Orders.Create(ord).Result);
		}

		/*addCustomer
		 * adds a customer to server and saves it to local list
		 * */
		public void addCustomer(Customer cust){
			CustList.Add(Customers.Create(cust).Result);
		}

		/*addReview
		 * adds an review to server and saves it to local list
		 * */
		public void addReview(Review rev){
			RevList.Add(Reviews.Create(rev).Result);
		}

		/*scenario
		 * creates a new scenario on server with params given to TestFixture
		 * */
		public void scenario(){
			//add customers
			addCustomer (new Customer () {
				name = "Young A",
				age = 22
			});
			addCustomer (new Customer(){
				name = "Young B",
				age = 19
			});
			for (int i = 0; i < 3; i++) {
				addCustomer (new Customer () {
					name = "Customer " + i,
					age = 50 + i
				});
				for(int j = 0; j < revs[i]; j++){
					addReview (new Review () {
						star = 3,
						authorId = Convert.ToInt16 (CustList [i+2].id),
						product = "Product " + RevList.Count
					});
				}
				for(int j = 0; j < ords[i]; j++){
					addOrder (
						new Order () {
							total = 100,
							description = "Order " + OrdList.Count,
							customerId = Convert.ToInt16 (CustList [i + 2].id)
						});
				}
					
			}
			custToDeletein = CustList [2].getID ();
			custToDestroyIn = CustList [3].getID ();
			custForOuterRelations = CustList [4].getID ();

			orderToDelete = OrdList [ords[0]].getID();
			orderToFind = OrdList [ords[0] + 1].getID ();
			orderRelationsDestroy = OrdList [ords[0] + ords[1] + ords[2] - 1].getID ();
			orderToGetFor = OrdList [ords[0] + ords[1] + ords[2] - 2].getID ();

			reviewToDelete = RevList [revs[0]].getID ();
			reviewToFind = RevList [revs[0] +1].getID ();
			reviewRelationsDestroy = RevList [revs[0] + revs[1] + revs[2] - 1].getID();
			reviewToGetFor = RevList [revs[0] + revs[1] + revs[2] - 2].getID();
		}
		#endregion

		#region fixture properties for setup/teardown
		[TestFixtureSetUp]
		public void fixtureSetUp(){
			Gateway.SetServerBaseURLToSelf ();
			OldOrdList = Orders.Find ().Result;
			OldRevList = Reviews.Find ().Result;
			OldCustList = Customers.Find ().Result;
			deleteAllOrders ();
			deleteAllCustomers ();
			deleteAllReviews ();
			scenario ();
		}
		[TestFixtureTearDown]
		public void fixtureTearDown(){
			deleteAllOrders ();
			deleteAllCustomers ();
			deleteAllReviews ();
			foreach (var order in OldOrdList) {
				Orders.Create (order).Wait();
			}
			foreach (var customer in OldCustList) {
				Customers.Create (customer).Wait();
			}
			foreach (var review in OldRevList) {
				Reviews.Create (review).Wait();
			}
		}
		#endregion


		//Relationship functions
		#region Customer Relations
		//Customer CountOrders
		[Test]
		public void relation_a10_CustomerCountOrders(){
			Assert.AreEqual(ords[0], Customers.countOrders (CustList[2].getID(), "").Result);
			Assert.AreEqual(ords[1], Customers.countOrders (CustList[3].getID(), "").Result);
		}

		[Test]
		//Customer Count reviews
		public void relation_a11_CustomerCountReviews(){
			Assert.AreEqual(revs[0], Customers.countReviews (CustList[2].getID(), "").Result);
			Assert.AreEqual(revs[1], Customers.countReviews (CustList[3].getID(), "").Result);
		}

		[Test]
		//Customer getOrders
		public void relation_a111_CustomerGetOrders(){
			IList<Order> getList = Customers.getOrders (custToDeletein, "").Result;
			Assert.AreNotEqual (null, getList);
			Assert.AreEqual (ords[0], getList.Count);
			int i = 0;
			foreach (Order ord in getList){
				Assert.AreEqual(OrdList[i].total, ord.total);
				Assert.AreEqual(OrdList[i].customerId, ord.customerId);
				Assert.AreEqual(OrdList[i].description, ord.description);
				Assert.AreEqual(OrdList[i].id, ord.id);
				++i;
			}
		}

		[Test]
		//Customer getOrders
		public void relation_a112_CustomerGetReviews(){
			IList<Review> getList = Customers.getReviews (custToDeletein).Result;
			Assert.AreNotEqual (null, getList);
			Assert.AreEqual (revs[2], getList.Count);
			int i = 0;
			foreach (Review rev in getList){
				Assert.AreEqual(RevList[i].star, rev.star);
				Assert.AreEqual(RevList[i].product, rev.product);
				Assert.AreEqual(RevList[i].authorId, rev.authorId);
				Assert.AreEqual(RevList[i].id, rev.id);
				++i;
			}

		}

		[Test]
		//Customer getOrders
		public void  relation_a113_CustomerGetYoung(){
			IList<Customer> getList = Customers.getYoungFolks ().Result;
			Assert.AreNotEqual (null, getList);
			Assert.AreEqual (2, getList.Count);
			int i = 0;
			foreach (Customer cust in getList){
				Assert.AreEqual(CustList[i].age, cust.age);
				Assert.AreEqual(CustList[i].name, cust.name);
				Assert.AreEqual(CustList[i].id, cust.id);
				++i;
			}

			getList = Customers.getYoungFolks ("{\"where\": {\"age\": {\"gt\": 19}}}").Result;
			Assert.AreNotEqual (null, getList);
			Assert.AreEqual (1, getList.Count);
			foreach (Customer cust in getList){
				Assert.AreEqual(CustList[0].age, cust.age);
				Assert.AreEqual(CustList[0].name, cust.name);
				Assert.AreEqual(CustList[0].id, cust.id);
			}

		}
		[Test]
		//Customer Count YoungFolk
		public void relation_a12_CustomerCountYoungFolk(){
			Assert.AreEqual (2, Customers.countYoungFolks ().Result);		}

		[Test]
		//Customer create order
		public void relation_a13_CustomerCreateOrder(){
			Order newOrder = new Order () {
				description = "Order 6",
				total = 6
			};
			string custId = CustList [3].getID();
			Order createdOrder = Customers.createOrders(newOrder, custId).Result;
			Assert.AreNotEqual (null, createdOrder);
			Assert.AreEqual (Convert.ToInt16(custId), createdOrder.customerId);
			Assert.AreEqual (newOrder.description, createdOrder.description);
			Assert.AreEqual (newOrder.total, createdOrder.total);
			Orders.DeleteById (createdOrder.getID ()).Wait();

		}

		[Test]
		//Customer Create Review
		public void relation_a14_CustomerCreateReview(){
			Review newRev = new Review () {
				product = "productX",
				star = 3
			};
			string custId = CustList [3].getID();
			Review createdRev = Customers.createReviews (newRev, custId).Result;
			Assert.AreNotEqual (null, createdRev);
			Assert.AreEqual (Convert.ToInt16(custId) , createdRev.authorId);
			Assert.AreEqual (newRev.product, createdRev.product);
			Assert.AreEqual (newRev.star, createdRev.star);
			Reviews.DeleteById (createdRev.getID ()).Wait();
		}

		[Test]
		//Customer Delete order
		public void relation_a15_CustomerDeleteOrder(){

			Customers.deleteOrders (custToDeletein).Wait();
			Assert.AreEqual(0, Customers.countOrders (custToDeletein).Result);
			IList<Order> orders = Orders.Find("{\"where\": {\"customerId\": \"" + custToDeletein + "\"}}").Result;
			Assert.AreNotEqual (null, orders);
			Assert.AreEqual (0, orders.Count);
		}

		[Test]
		//Customer Delete Reviews
		public void relation_a16_CustomerDeleteReview(){
			Customers.deleteReviews (custToDeletein).Wait();
			Assert.AreEqual(0, Customers.countReviews (custToDeletein).Result);
			IList<Order> reviews = Orders.Find("{\"where\": {\"authorId\": \"" + custToDeletein + "\"}}").Result;
			Assert.AreNotEqual (null, reviews);
			Assert.AreEqual (0, reviews.Count);
		}

		[Test]
		//Customer delete YoungFolk
		public void relation_a17_CustomerDeleteYoung(){
			Customers.deleteYoungFolks ().Wait();
			Assert.AreEqual (0, Customers.countYoungFolks ().Result);
			IList<Customer> youngs = Customers.getYoungFolks ().Result;
			Assert.AreNotEqual (null, youngs);
			Assert.AreEqual (0, youngs.Count);
		}

		[Test]
		//Customer destroyByOrders
		public void relation_a18_CustomerDestroyByOrdersID(){
			Customers.destroyByIdOrders (custToDestroyIn, orderToDelete).Wait();
            try
            {
                Order order = Orders.FindById(orderToDelete).Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(404, restException.StatusCode);
            }
		}

		[Test]
		//Customer destroyByOrders
		public void relation_a19_CustomerDestroyByReviewsID(){
			Customers.destroyByIdReviews (custToDestroyIn, reviewToDelete).Wait();
            try
            {
                Review review = Reviews.FindById(reviewToDelete).Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(404, restException.StatusCode);
            }
		}

		[Test]
		//Customer findbyOrderIds
		public void relation_a20_CustomerFindByOrderIds(){
			Order foundOrder = Customers.findByIdOrders (custToDestroyIn, orderToFind).Result;
			Order realOrder = Orders.FindById (orderToFind).Result;
			Assert.AreNotEqual (null, foundOrder);
			Assert.AreEqual (realOrder.description, foundOrder.description);
			Assert.AreEqual (realOrder.total, foundOrder.total);
			Assert.AreEqual (realOrder.getID(), foundOrder.getID ());
			Assert.AreEqual (realOrder.customerId, foundOrder.customerId);
		}

		[Test]
		//Customer FindByReviewIds
		public void relation_a21_CustomerFindByReviewIds(){
			Review foundReview = Customers.findByIdReviews (custToDestroyIn, reviewToFind).Result;
			Review realReview = Reviews.FindById (reviewToFind).Result;
			Assert.AreNotEqual (null, foundReview);
			Assert.AreEqual (realReview.getID(), foundReview.getID());
			Assert.AreEqual (realReview.authorId, foundReview.authorId);
			Assert.AreEqual (realReview.product, foundReview.product);
			Assert.AreEqual (realReview.star, foundReview.star);
		}
		#endregion

		//Order relations
		#region Order Relations
		[Test]
		//Order count for customer
		public void relation_o10_OrderCountForCustomer(){
			Assert.AreEqual (ords[2], Orders.countForCustomer(custForOuterRelations).Result);
		}

		[Test]
		//Order createForCustomer
		public void relation_o11_OrderCreateForCustomer(){
			Order newOrder = new Order () {
				description = "prodXX",
				total = 13
			};
			Order createdOrder = Orders.createForCustomer(newOrder, custForOuterRelations).Result;
			Assert.AreEqual(Convert.ToInt16(custForOuterRelations), createdOrder.customerId); 
			Assert.AreEqual(newOrder.description, createdOrder.description); 
			Assert.AreEqual(newOrder.total, createdOrder.total); 
			Assert.AreEqual (ords[2] + 1, Orders.countForCustomer(custForOuterRelations).Result);
			Orders.DeleteById (createdOrder.id).Wait();
		}

		[Test]
		//Order delForCustomer
		public void relation_o12_OrderDeleteForCustomer(){
			Orders.deleteForCustomer (custToDestroyIn).Wait();
			Assert.AreEqual (0, Orders.countForCustomer(custToDestroyIn).Result);
		}

		[Test]
		//Order destroyByIdForCustomer
		public void relation_o13_OrderDestroyByIdForCustomer(){
			Orders.destroyByIdForCustomer (custForOuterRelations, orderRelationsDestroy).Wait();
			Assert.AreEqual (ords[2] - 1, Orders.countForCustomer(custForOuterRelations).Result);
		}

		[Test]
		//Order GetCustomer
		public void relation_o14_OrderGetCustomer(){
			Customer getCust = Orders.getCustomer (orderToGetFor).Result;
			Customer realCust = Customers.FindById (custForOuterRelations).Result;
			Assert.AreEqual(realCust.getID(), getCust.getID());
			Assert.AreEqual(realCust.age, getCust.age);
			Assert.AreEqual(realCust.name, getCust.name);
		}

		[Test]
		//Order GetForCustomer
		public void relation_o15_OrderGetForCustomer(){
			IList<Order> ordListServer = Orders.getForCustomer (custForOuterRelations).Result;
			IList<Order> ordListReal = Customers.getOrders (custForOuterRelations).Result;
			for (int i = 0; i < ordListReal.Count; i++) {
				Order o1 = ordListReal [i];
				Order o2 = ordListServer [i];
				Assert.AreEqual (o1.customerId, o2.customerId);
				Assert.AreEqual (o1.description, o2.description);
				Assert.AreEqual (o1.total, o2.total);
			}
		}
		#endregion

		//Review Relations
		#region Review Relations
		[Test]
		//Review count for customer
		public void relation_o10_ReviewCountForCustomer(){
			Assert.AreEqual (revs[2], Reviews.countForCustomer(custForOuterRelations).Result);

		}

		[Test]
		//Review createForCustomer
		public void relation_o11_ReviewCreateForCustomer(){
			Review newReview = new Review () {
				product = "newProd",
				star = 3
			};
			Review createdReview = Reviews.createForCustomer (newReview, custForOuterRelations).Result;
			Assert.AreEqual (revs[2] + 1, Reviews.countForCustomer(custForOuterRelations).Result);
			Assert.AreEqual (newReview.product, createdReview.product);
			Assert.AreEqual (newReview.star, createdReview.star);
			Assert.AreEqual (Convert.ToInt16 (custForOuterRelations), createdReview.authorId);
			Reviews.DeleteById (createdReview.id).Wait();
		}

		[Test]
		//Review delForCustomer
		public void relation_o12_ReviewDeleteForCustomer(){
			Orders.deleteForCustomer (custToDestroyIn).Wait();
			Assert.AreEqual (0, Orders.countForCustomer(custToDestroyIn).Result);
		}

		[Test]
		//Review destroyByIdForCustomer
		public void relation_o13_ReviewDestroyByIdForCustomer(){
			//destById
			Reviews.destroyByIdForCustomer (custForOuterRelations, reviewRelationsDestroy).Wait();
			Assert.AreEqual (revs[2] - 1, Reviews.countForCustomer(custForOuterRelations).Result);
		}

		[Test]
		//Review GetAuthor
		public void relation_o14_ReviewGetAuthor(){
			Customer getCust = Reviews.getAuthor (reviewToGetFor).Result;
			Customer realCust = Customers.FindById (custForOuterRelations).Result;
			Assert.AreEqual(realCust.getID(), getCust.getID());
			Assert.AreEqual(realCust.age, getCust.age);
			Assert.AreEqual(realCust.name, getCust.name);
		}

		[Test]
		//Review GetForCustomer
		public void relation_o15_ReviewGetForCustomer(){
			IList<Review> revListServer = Reviews.getForCustomer (custForOuterRelations).Result;
			IList<Review> revListReal = Customers.getReviews (custForOuterRelations).Result;
			for (int i = 0; i < revListReal.Count; i++) {
				Review r1 = revListReal [i];
				Review r2 = revListReal [i];
				Assert.AreEqual (r1.authorId, r2.authorId);
				Assert.AreEqual (r1.product, r2.product);
				Assert.AreEqual (r1.star, r2.star);
			}
		}

		[Test]
		//Review destroyByIdForCustomer
		public void relation_o16_ReviewFindByIdForCustomer(){

			IList<Review> revListServer = Reviews.getForCustomer (custForOuterRelations).Result;
			foreach(Review rev in revListServer){
				Review o1 = Reviews.findByIdForCustomer (custForOuterRelations, rev.id).Result;
				Assert.AreEqual (rev.authorId, o1.authorId);
				Assert.AreEqual (rev.product, o1.product);
				Assert.AreEqual (rev.star, o1.star);
			}
		}
		#endregion
	}
}

