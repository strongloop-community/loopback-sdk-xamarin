using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System;
namespace UnitTests
{
	/*
	 * b_TestMiniUserCRUDAsAdmin class
	 * 
	 * Tests the server for miniUser CRUD functions as an admin
	 * Follows a predefined server state
	 * Run with a new server instance, otherwise ambigous results will occure
	 */
	[TestFixture]
	public class b_TestMiniUserCRUDAsAdmin{
		/*	perfomLogin
		 * preforms login as admin
		 */
		public void performLogin(){
			MiniUser credentials = new MiniUser () {
				email = "admin@g.com",
				password = "1234"
			};
			var accessToken = MiniUsers.login (credentials).Result;
		
			Gateway.SetAccessToken (accessToken);
		}
		#region testfixture setup/teardown
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

		#region CRUD test
		//Count
		[Test]
		public void a10_count(){
			Assert.AreEqual (3, MiniUsers.Count ().Result);
		}

		//FindById
		[Test]
		public void a11_findById(){
			MiniUser usr = MiniUsers.FindById ("1").Result;
			Assert.AreNotEqual (null, usr);
			Assert.AreEqual ("admin@g.com", usr.email);
			usr = MiniUsers.FindById ("2").Result;
			Assert.AreNotEqual (null, usr);
			Assert.AreEqual ("admin1@g.com", usr.email);

		}

		//Create and Delete
		[Test]
		public void a12_createDelete(){
			MiniUser newMiniUser = new MiniUser () {
				email = "newMiniUser2@g.com",
				password = "1234"
			};
			newMiniUser = MiniUsers.Create (newMiniUser).Result;
			string id = newMiniUser.id;
			Assert.AreEqual ("newMiniUser2@g.com", newMiniUser.email);
			//trying to create duplicate MiniUser
            try
            {
                MiniUser usr = MiniUsers.Create(newMiniUser).Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(422, restException.StatusCode);
            }
            MiniUsers.DeleteById(id).Wait();
            try
            {
                MiniUser delRes = MiniUsers.FindById(id).Result;
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                RestException restException = (RestException)e.InnerException;
                Assert.AreEqual(404, restException.StatusCode);
            }
		}
			
		//Find
		[Test]
		public void a13_find(){
			IList<MiniUser> usrList = MiniUsers.Find ().Result;
			Assert.AreNotEqual (null, usrList);
			int i = 1;
			foreach (MiniUser usr in usrList) {
				Assert.AreEqual (i.ToString (), usr.getID ());
				i++;
			}
		}

		//Exists
		[Test]
		public void a14_exists(){
			Assert.AreEqual(true, MiniUsers.Exists("1").Result);
			Assert.AreEqual(false, MiniUsers.Exists("10").Result);
		}

		//FindOne
		[Test]
		public void a15_findOne(){
			MiniUser usr = MiniUsers.FindOne ().Result;
			Assert.AreNotEqual(null, usr);
			Assert.AreEqual ("1", usr.getID());

			usr = MiniUsers.FindOne("{\"where\": {\"email\" : \"admin1@g.com\"}}").Result;
			Assert.AreNotEqual(null, usr);
			Assert.AreEqual ("2", usr.getID());


		}
		//updateAllById
		[Test]
		public void a16_updateAll_ById(){
			MiniUser upUsr = new MiniUser () {
				email = "updateAll@g.com"
			};
			MiniUsers.UpdateAll(upUsr, "{\"id\" : \"3\"}").Wait();
			MiniUser upRes = MiniUsers.FindById ("3").Result;
			Assert.AreEqual (upUsr.email, upRes.email);

			MiniUser upUsr3 = new MiniUser () {
				email = "admin2@g.com"
			};
			MiniUsers.UpdateById ("3", upUsr3).Wait();

			upRes = MiniUsers.FindById ("3").Result;
			Assert.AreEqual (upUsr3.email, upRes.email);
		}
		#endregion
	}
}

