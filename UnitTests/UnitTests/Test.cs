using System;
using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;

namespace UnitTests
{

	[TestFixture]
	public class a_TestCRUDAllButUser
	{
		#region params
		#region customer params
		//params for Customer CRUD tests
		//filters
		string customerFilter = "{\"where\": {\"and\": [{\"age\": {\"gt\": 22}} ,{\"age\": {\"lt\": 25}}]}}";
		string customerBadFilter = "{\"where\": {\"id\": \"1000\"}}"; //filter that should produce 0 results
		string customerWhereFilter = "{\"and\": [{\"age\": {\"gt\": 22}} ,{\"age\": {\"lt\": 25}}]}";
		//customer objects for creatin/updates
		Customer updateCustomerById = new Customer {
			name = "sdfsd"
		};
		Customer updateCustomerAll = new Customer () {
			name = "Jimbob"
		};
		Customer newCustomer = new Customer () {
			age = 121,
			name = "testCreate"
		};
		#endregion	
		//params for Review CRUD tests
		#region Review params
		//filters
		string reviewFilter = "{\"where\": {\"star\": {\"lte\": 4}} }";
		string reviewBadFilter = "{\"where\": {\"id\": \"1000\"}}";
		string reviewWhereFilter = "{\"star\": {\"lte\": 4}}";
		//customer objects for creatin/updates
		Review updateReviewById = new Review () {
			product = "productXXX",
			authorId = 2,
			star = 1
		};
		Review updateReviewAll = new Review () {
			star = 1
		};
		Review newReview = new Review () {
			product = "newProduct",
			star = 3, 
			authorId = 1
		};
		#endregion
		//params for Order CRUD tests
		#region Order params
		//filters
		string orderFilter = "{\"where\": {\"total\": {\"lte\": 200}} }";
		string orderBadFilter =  "{\"where\": {\"id\": \"1000\"}}";
		string orderWhereFilter = "{\"total\": {\"lte\": 200}}";
		//order objects for creatin/updates
		static Order updateOrderById = new Order () {
			description = "OrderXX",
			customerId = 4,
			total = 20
		};
		static Order updateOrderAll = new Order () {
			total = 100
		};
		static Order newOrder = new Order () {
			description = "OrderXXX",
			customerId = 4,
			total = 300.5
		};
		#endregion
		#endregion

		#region testfixture setup/teardown
		[TestFixtureSetUp]
		public  void Setup(){
			Gateway.SetServerBaseURLToSelf ();
		}
		#endregion

		//CRUD Tests

		//Customers
		#region Customers CRUD
		//Customer Count test
		[Test]
		public void crud_c0_CustomerCount(){
			Assert.AreEqual (5, Customers.Count ().Result);
		}

		//Customer Exists test
		[Test]
		public void crud_c1_CustomerExists(){
			Assert.AreEqual(true, Customers.Exists ("1").Result);
			Assert.AreEqual(false, Customers.Exists ("7").Result);
		}

		//Customer FindById
		[Test]
		public void crud_c11_CustomerFindById(){
			Customer foundCustomer = Customers.FindById ("4").Result;
			Assert.AreNotEqual (null, foundCustomer);
			Assert.AreEqual ("Customer D", foundCustomer.name);
			Assert.AreEqual ("4", foundCustomer.getID());
			Assert.AreEqual (24, foundCustomer.age);
		}
			
		[Test]
		//Customer Find
		public void crud_c12_CustomerFind(){
			IList<Customer> CustList = Customers.Find ("").Result;
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
		public void crud_c13_CustomerFindWithFilter(){

			IList<Customer> CustList = Customers.Find (customerFilter).Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (2, CustList.Count);
			int[] arr = { 3, 4 };
			int i = 0;
			foreach (Customer cust in CustList){
				string str = arr[i].ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}
		
			CustList = Customers.Find (customerBadFilter).Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (0, CustList.Count); 
		}

		//Customer FindOne
		[Test]
		public void crud_c14_CustomerFindOne(){
			Customer cust = Customers.FindOne (customerBadFilter).Result;
			Assert.AreEqual (null, cust);
			cust = Customers.FindOne ().Result;
			Assert.AreNotEqual (null, cust);
			Assert.AreEqual ("1", cust.getID ());

			cust = CRUDInterface<Customer>.FindOne (customerFilter).Result;
			Assert.AreNotEqual (null, cust);
			Assert.AreEqual ("3", cust.getID ());
		}

		//Customer UpdateById
		[Test]
		public void crud_c15_CustomerUpdateById(){

			Customer beforeUpdate = Customers.FindById ("3").Result;
			Customer upResult = Customers.UpdateById ("3", updateCustomerById).Result;

			Assert.AreNotEqual (null, upResult);
			Assert.AreEqual ("3", upResult.getID ());
			Assert.AreEqual(updateCustomerById.name, upResult.name);
			Assert.AreNotEqual (0, upResult.age);
			//update back
			Customers.UpdateById ("3", beforeUpdate).Wait ();

		}

		//Customer UpdateAll
		[Test]
		public void crud_c16_CustomerUpdateAll(){
			Customers.UpdateAll (updateCustomerAll, customerWhereFilter).Wait ();
			IList<Customer> CustList = Customers.Find (customerFilter).Result;
			Assert.AreNotEqual (null, CustList);
			Assert.AreEqual (2, CustList.Count);
			foreach (Customer cust in CustList){
				Assert.AreEqual(updateCustomerAll.name, cust.name);
				Assert.AreNotEqual (updateCustomerAll.age, cust.age);
			}
		}

		//Customer create
		[Test]
		public void crud_c17_CustomerCreate(){


			Customer newCustomerResp = Customers.Create(newCustomer).Result;
			Assert.AreNotEqual (null, newCustomerResp);
			Assert.AreEqual (newCustomer.age, newCustomerResp.age);
			Assert.AreEqual (newCustomer.name, newCustomerResp.name);
			Assert.AreEqual (6, Customers.Count ().Result);
		}

		//Customer delete
		[Test]
		public void crud_c18_CustomerDelete(){
			Customers.DeleteById ("6").Wait();
			Assert.AreEqual (false, Customers.Exists ("6").Result);
		}

		//Customer upsert
		[Test]
		public void crud_c19_CustomerUpsert(){
			//Upsert new customer
			Customer upsertResult = Customers.Upsert (newCustomer).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newCustomer.age, upsertResult.age);
			Assert.AreEqual (newCustomer.name, upsertResult.name);
			Assert.AreEqual (6, Customers.Count ().Result);
			//Upsert update customer
			string newId = upsertResult.getID ();
			Customer upCustomer = new Customer () {
				age = 150,
				name = "Jimmy",
				id = newId
			};
			upsertResult = Customers.Upsert (upCustomer).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (upCustomer.age, upsertResult.age);
			Assert.AreEqual (upCustomer.name, upsertResult.name);

		}
		#endregion
		//Reviews

		#region Reviews
		//Reviews Count test
		[Test]
		public void crud_r_ReviewsCount(){
			Assert.AreEqual (5, Reviews.Count ().Result);
		}

		//Reviews Exists test
		[Test]
		public void crud_r10_ReviewsExists(){
			Assert.AreEqual(true, Reviews.Exists ("3").Result);
			Assert.AreEqual(false, Reviews.Exists ("9").Result);
		}

		//Reviews FindById
		[Test]
		public void crud_r11_ReviewsFindById(){
			Review foundReview = Reviews.FindById ("4").Result;
			Assert.AreNotEqual (null, foundReview);
			Assert.AreEqual ("Product4", foundReview.product);
			Assert.AreEqual (2, foundReview.star);
			Assert.AreEqual (2, foundReview.authorId);
			Assert.AreEqual ("4", foundReview.id);
		}

		[Test]
		//Reviews Find
		public void crud_r12_ReviewsFind(){
			IList<Review> CustList = Reviews.Find ("").Result;
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
		public void crud_r13_ReviewsFindWithFilter(){
			IList<Review> RevList = Reviews.Find (reviewFilter).Result;
			Assert.AreNotEqual (null, RevList);
			Assert.AreEqual (3, RevList.Count);
			int[] arr = { 1, 2, 4 };
			int i = 0;
			foreach (Review cust in RevList){
				string str = arr[i].ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}
				
			RevList = Reviews.Find (reviewBadFilter).Result;
			Assert.AreNotEqual (null, RevList);
			Assert.AreEqual (0, RevList.Count); 
		}

		//Reviews FindOne
		[Test]
		public void crud_r14_ReviewsFindOne(){
			Review rev = Reviews.FindOne (reviewBadFilter).Result;
			Assert.AreEqual (null, rev);

			rev = Reviews.FindOne ().Result;
			Assert.AreNotEqual (null, rev);
			Assert.AreEqual ("1", rev.getID ());

			rev = CRUDInterface<Review>.FindOne ( "{\"where\": {\"star\": {\"gte\": 4}} }").Result;
			Assert.AreNotEqual (null, rev);
			Assert.AreEqual ("2", rev.getID ());
		}

		//Reviews UpdateById
		[Test]
		public void crud_r15_ReviewsUpdateById(){


			Review upResult = Reviews.UpdateById ("1", updateReviewById).Result;
			Assert.AreNotEqual (null, upResult);
			Assert.AreEqual ("1", upResult.getID ());
			Assert.AreEqual(updateReviewById.product, upResult.product);
			Assert.AreEqual (updateReviewById.star, upResult.star);
			Assert.AreEqual (updateReviewById.authorId, upResult.authorId);
		}

		//Reviews UpdateAll
		[Test]
		public void crud_r16_ReviewsUpdateAll(){
			//string whereFilter = "{\"star\": {\"lte\": 4}}";

			Reviews.UpdateAll (updateReviewAll, reviewWhereFilter).Wait();
			//string filter = "{\"where\": {\"star\": {\"lte\": 1}}}";
			IList<Review> RevList = Reviews.Find (reviewFilter).Result;
			Assert.AreNotEqual (null, RevList);
			Assert.AreEqual (3, RevList.Count);
			foreach (Review rev in RevList){
				Assert.AreEqual(updateReviewAll.star, rev.star);
			}
		}

		//Reviews create
		[Test]
		public void crud_r17_ReviewsCreate(){

			Review newRevResp = Reviews.Create(newReview).Result;
			Assert.AreNotEqual (null, newRevResp);
			Assert.AreEqual (newReview.product, newRevResp.product);
			Assert.AreEqual (newReview.star, newRevResp.star);
			Assert.AreEqual (newReview.authorId, newRevResp.authorId);
			Assert.AreEqual (6, Reviews.Count ().Result);
		}

		//Reviews delete
		[Test]
		public void crud_r18_ReviewsDelete(){
			Reviews.DeleteById ("6");
			Assert.AreEqual (false, Reviews.Exists ("6").Result);
		}

		//Reviews upsert
		[Test]
		public void crud_r19_ReviewsUpsert(){
			//Upsert new Reviews
			Review upsertResult = Reviews.Upsert (newReview).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newReview.product, upsertResult.product);
			Assert.AreEqual (newReview.star, upsertResult.star);
			Assert.AreEqual (newReview.authorId, upsertResult.authorId);
			Assert.AreEqual (6, Reviews.Count ().Result);

			//Upsert update Reviews
			string newId = upsertResult.getID ();
			Review upReview = new Review () {
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

		}
		#endregion

		//Orders
		#region Orders
		//Orders Count test
		[Test]
		public void crud_o_OrdersCount(){
			Assert.AreEqual (5, Orders.Count ().Result);
		}

		//Orders Exists test
		[Test]
		public void crud_o10_OrdersExists(){
			Assert.AreEqual(true, Orders.Exists ("3").Result);
			Assert.AreEqual(false, Orders.Exists ("9").Result);
		}

		//Orders FindById
		[Test]
		public void crud_o11_OrdersFindById(){
			Order foundOrder = Orders.FindById ("4").Result;
			Assert.AreNotEqual (null, foundOrder);
			Assert.AreEqual ("Order D", foundOrder.description);
			Assert.AreEqual (150.45, foundOrder.total);
			Assert.AreEqual (2, foundOrder.customerId);
			Assert.AreEqual ("4", foundOrder.id);
		}

		[Test]
		//Orders Find
		public void crud_o12_OrdersFind(){
			IList<Order> OrdList = Orders.Find ("").Result;
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
		public void crud_o13_OrdersFindWithFilter(){

			IList<Order> OrdList = Orders.Find (orderFilter).Result;
			Assert.AreNotEqual (null, OrdList);
			Assert.AreEqual (3, OrdList.Count);
			int[] arr = { 2, 4, 5 };
			int i = 0;
			foreach (Order cust in OrdList){
				string str = arr[i].ToString();
				Assert.AreEqual(str, cust.getID());
				++i;
			}

			OrdList = Orders.Find (orderBadFilter).Result;
			Assert.AreNotEqual (null, OrdList);
			Assert.AreEqual (0, OrdList.Count); 
		}

		//Orders FindOne
		[Test]
		public void crud_o14_OrdersFindOne(){
			Order ord = Orders.FindOne (orderBadFilter).Result;
			Assert.AreEqual (null, ord);

			ord = Orders.FindOne ().Result;
			Assert.AreNotEqual (null, ord);
			Assert.AreEqual ("1", ord.getID ());

			ord =Orders.FindOne (orderFilter).Result;
			Assert.AreNotEqual (null, ord);
			Assert.AreEqual ("2", ord.getID ());
		}

		//Orders UpdateById
		[Test]
		public void crud_o15_OrdersUpdateById(){
			Order upResult = Orders.UpdateById ("5", updateOrderById).Result;
			Assert.AreNotEqual (null, upResult);
			Assert.AreEqual ("5", upResult.getID ());
			Assert.AreEqual(updateOrderById.description, upResult.description);
			Assert.AreEqual (updateOrderById.customerId, upResult.customerId);
			Assert.AreEqual (updateOrderById.total, upResult.total);
		}

		//Orders UpdateAll
		[Test]
		public void crud_o16_OrdersUpdateAll(){


			Orders.UpdateAll (updateOrderAll, orderWhereFilter).Wait();
			IList<Order> ordList = Orders.Find (orderFilter).Result;
			Assert.AreNotEqual (null, ordList);
			Assert.AreEqual (3, ordList.Count);
			foreach (Order ord in ordList){
				Assert.AreEqual(updateOrderAll.total, ord.total);
			}

		}

		//Orders create
		[Test]
		public void crud_o17_OrdersCreate(){

			Order newOrdResp = Orders.Create(newOrder).Result;
			Assert.AreNotEqual (null, newOrdResp);
			Assert.AreEqual (newOrder.description, newOrdResp.description);
			Assert.AreEqual (newOrder.customerId, newOrdResp.customerId);
			Assert.AreEqual (newOrder.total, newOrdResp.total);
			Assert.AreEqual (6, Orders.Count ().Result);
		}

		//Orders delete
		[Test]
		public void crud_o18_OrdersDelete(){
			Orders.DeleteById ("6");
			Assert.AreEqual (false, Orders.Exists ("6").Result);
		}

		//Orders upsert
		[Test]
		public void crud_o19_OrdersUpsert(){
			//Upsert new Orders
			Order upsertResult = Orders.Upsert (newOrder).Result;
			Assert.AreNotEqual (null, upsertResult);
			Assert.AreEqual (newOrder.description, upsertResult.description);
			Assert.AreEqual (newOrder.customerId, upsertResult.customerId);
			Assert.AreEqual (newOrder.total, upsertResult.total);
			Assert.AreEqual (6, Orders.Count ().Result);

			//Upsert update Reviews
			string newId = upsertResult.getID ();
			newOrder = new Order () {
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

		}
		#endregion

			
	}
		
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
	[TestFixture(new int[] { 3, 3, 3 },new int[] { 3, 3, 3 })]
	[TestFixture(new int[] { 4, 4, 4 },new int[] { 4,4, 4 })]
	public class e_TestRelations{
		/*Constructor
		 * input: 
		 * 	ords - array of orders per Customer
		 * 	revs - array of reviews per customer
		 * output:
		 * 	new test
		 * */
		public e_TestRelations(int[] ords = null, int[] revs = null)
		{
			this.ords = ords != null && ords.Length == 3 ? ords : new int[]{ 3, 3, 3 };
			this.revs = revs != null && revs.Length == 3 ? revs : new int[]{ 3, 3, 3 };

		}
		#region params
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
			Order createdOrder = Customers.createOrders(custId, newOrder).Result;
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
			Review createdRev = Customers.createReviews (custId, newRev).Result;
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
			Order order = Orders.FindById (orderToDelete).Result;
			Assert.AreEqual (null, order);
		}

		[Test]
		//Customer destroyByOrders
		public void relation_a19_CustomerDestroyByReviewsID(){
			Customers.destroyByIdReviews (custToDestroyIn, reviewToDelete).Wait();
			Review review = Reviews.FindById (reviewToDelete).Result;
			Assert.AreEqual (null, review);
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
			Order createdOrder = Orders.createForCustomer(custForOuterRelations, newOrder).Result;
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
			Review createdReview = Reviews.createForCustomer (custForOuterRelations, newReview).Result;
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

