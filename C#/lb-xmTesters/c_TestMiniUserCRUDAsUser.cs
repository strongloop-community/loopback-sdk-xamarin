using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System;

namespace UnitTests
{
	/*
	 * c_TestMiniUserCRUDAsUser class
	 * 
	 * Tests the server for miniUser CRUD functions as a regular user
	 * Follows a predefined server state
	 * Run with a new server instance, otherwise ambigous results will occure
	 */
	[TestFixture]
	public class c_TestMiniUserCRUDAsUser{
		#region functions
		/*	performLoginAsAdmin
		 * performs login as server admin
		 */
		public void performLoginAsAdmin(){
			MiniUser credentials = new MiniUser () {
				email = "admin@g.com",
				password = "1234"
			};
			var a = MiniUsers.login (credentials).Result;

			Gateway.SetAccessToken (a);
		}
		/*	perfomLogin
		 * preforms login as admin
		 */

		public void performLogin(){
			MiniUser credentials = new MiniUser () {
				email = "admin1@g.com",
				password = "1234"
			};
			var a = MiniUsers.login (credentials).Result;

			Gateway.SetAccessToken (a);
		}
		#endregion
		#region testfixture setup/teardow
		[TestFixtureSetUp]
		public void setup(){
			Gateway.SetServerBaseURLToSelf ();
			performLogin ();
		}
            
		[TestFixtureTearDown]
		public void teardown(){
			MiniUsers.logout ().Wait();
		}
		#endregion
		#region CRUD tests
		//Count
		[Test]
		public void a10_count(){
            try
            {
                int result = MiniUsers.Count().Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}

		//Create
		[Test]
		public void a11_create(){
			MiniUser newMiniUser = new MiniUser () {
				email = "newMiniUser1@g.com",
				password = "1234"
			};
			MiniUser resMiniUser = MiniUsers.Create (newMiniUser).Result;
			string id = resMiniUser.id;
			Assert.AreEqual (resMiniUser.email, newMiniUser.email);
            try
            {
                newMiniUser = MiniUsers.Create(newMiniUser).Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(422, restException.StatusCode);
            }
			performLoginAsAdmin ();
			MiniUsers.DeleteById (id).Wait();
			performLogin ();
		}

		//findById
		[Test]
		public void a12_findById(){
			MiniUser usr = MiniUsers.FindById ("2").Result;
			Assert.AreNotEqual (null, usr);
			Assert.AreEqual ("admin1@g.com", usr.email);

            try
            {
                usr = MiniUsers.FindById("1").Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }

		}

		//Find
		[Test]
		public void a13_find(){
            try
            {
                IList<MiniUser> usrList = MiniUsers.Find().Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}

		//Exists
		[Test]
		public void a14_exists(){
            try
            {
                bool result = MiniUsers.Exists("1").Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
            try
            {
                bool result = MiniUsers.Exists("2").Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}

		//FindOne
		[Test]
		public void a15_findOne(){
            try
            {
                MiniUser result = MiniUsers.FindOne().Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}

		//update all
		[Test]
		public void a16_updateAll(){
			MiniUser upUsr = new MiniUser () {
				email = "update@g.com"
			};
            try
            {
                MiniUsers.UpdateAll(upUsr, "{\"id\" : \"3\"}").Wait();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}

		//updateById
		[Test]
		public void a17_updateById(){
			MiniUser upUsr = new MiniUser () {
				email = "update@g.com"
			};
            MiniUsers.UpdateById("2", upUsr).Wait();
            try
            {
                MiniUsers.UpdateById("3", upUsr).Wait();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
			MiniUser upRes = MiniUsers.FindById ("2").Result;
			Assert.AreEqual (upUsr.email, upRes.email);


			performLoginAsAdmin ();

			upRes = MiniUsers.FindById ("3").Result;
			Assert.AreNotEqual (upUsr.email, upRes.email);

			upUsr = new MiniUser () {
				email = "admin1@g.com"
			};

			MiniUsers.UpdateById ("2", upUsr).Wait();

			performLogin ();

		}

		//deleteById
		[Test]
		public void a18_deleteById(){
            try
            {
                MiniUsers.DeleteById("3").Wait();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}
		#endregion
	}
}

