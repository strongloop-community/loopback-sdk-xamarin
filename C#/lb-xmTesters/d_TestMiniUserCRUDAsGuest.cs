using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System;

namespace UnitTests
{
	/*
	 * d_TestMiniUserCRUDAsGuest class
	 * 
	 * Tests the server for miniUser CRUD functions as a guest
	 * Follows a predefined server state
	 * Run with a new server instance, otherwise ambigous results will occure
	 */
	[TestFixture]
	public class d_TestMiniUserCRUDAsGuest{
		[TestFixtureSetUp]
		public void setup(){
			Gateway.SetServerBaseURLToSelf ();
		}
		#region CRUD tests

		//Count
		[Test]
		public void a10_count(){
            try
            {
                int result = MiniUsers.Count ().Result;
                Assert.Fail();
            }
            catch(AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual (401, restException.StatusCode);
            }
		}

        //Create
		[Test]
		public void a11_create(){
			MiniUser newMiniUser = new MiniUser () {
				email = "new@g.com",
				password = "1234"
			};
			newMiniUser = MiniUsers.Create (newMiniUser).Result;
			Assert.AreEqual ("new@g.com", newMiniUser.email);
			//trying to create duplicate MiniUser
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
		}

		//FindById
		[Test]
		public void a13_findById(){
            try
            {
                MiniUser usr = MiniUsers.FindById("1").Result;
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
		public void a14_find(){
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
		public void a15_exists(){
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
		}

		//FindOne
		[Test]
		public void a16_findOne(){
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
		#endregion
	}
}

