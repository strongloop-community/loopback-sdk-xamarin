using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System;

namespace UnitTests
{
	/*
	 * TestMiniUserCRUDAsGuest class
	 * 
	 * Tests the server for miniUser CRUD functions as a guest
	 * Follows a predefined server state
	 * Run with a new server instance, otherwise ambigous results will occure
	 */
	[TestFixture]
	public class TestMiniUserCRUDAsGuest{
		#region TestFixture setup/teardown
		[TestFixtureSetUp]
		public void setup(){
			Gateway.SetServerBaseURLToSelf ();
		}
		#endregion

		#region functions
		/*	performLoginAsAdmin
		 * performs login as server admin
		 */
		public void performLoginAsAdmin(){
			var credentials = new MiniUser {
				email = "admin@g.com",
				password = "1234"
			};
			var a = MiniUsers.login (credentials).Result;

			Gateway.SetAccessToken (a);
		}
		#endregion	

		#region CRUD tests

		//Count
		[Test]
		public void count(){
            try
            {
                int result = MiniUsers.Count ().Result;
                Assert.Fail();
            }
            catch(AggregateException e)
            {
                var restException = (RestException)e.InnerException;
                Assert.AreEqual (401, restException.StatusCode);
            }
		}

        //Create
		[Test]
		public void create(){
			var newMiniUser = new MiniUser {
				email = "new@g.com",
				password = "1234"
			};
			newMiniUser = MiniUsers.Create (newMiniUser).Result;
			string id = newMiniUser.id;
			Assert.AreEqual ("new@g.com", newMiniUser.email);
			//trying to create duplicate MiniUser
            try
            {
                newMiniUser = MiniUsers.Create(newMiniUser).Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                var restException = (RestException)e.InnerException;
                Assert.AreEqual(422, restException.StatusCode);
            }
			performLoginAsAdmin ();
			MiniUsers.DeleteById (id).Wait();
			MiniUsers.logout ().Wait();
		}

		//FindById
		[Test]
		public void findById(){
            try
            {
                MiniUser usr = MiniUsers.FindById("1").Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                var restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }	
		}

		//Find
		[Test]
		public void find(){
            try
            {
                IList<MiniUser> usrList = MiniUsers.Find().Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                var restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }	
		}

		//Exists
		[Test]
		public void exists(){
            try
            {
                bool result = MiniUsers.Exists("1").Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                var restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}

		//FindOne
		[Test]
		public void findOne(){
            try
            {
                MiniUser result = MiniUsers.FindOne().Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                var restException = (RestException)e.InnerException;
                Assert.AreEqual(401, restException.StatusCode);
            }
		}
		#endregion
	}
}

