using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System;
namespace UnitTests
{
	/*
	 * TestMiniUserCRUDAsAdmin class
	 * 
	 * Tests the server for miniUser CRUD functions as an admin
	 * Follows a predefined server state
	 * Run with a new server instance, otherwise ambigous results will occure
	 */
	[TestFixture]
	public class TestMiniUserCRUDAsAdmin{
		#region functions
		/*	perfomLogin
		 * preforms login as admin
		 */
		public void performLogin(){
			var credentials = new MiniUser {
				email = "admin@g.com",
				password = "1234"
			};
			var accessToken = MiniUsers.login (credentials).Result;
		
			Gateway.SetAccessToken (accessToken);
		}
		#endregion

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
		public void count(){
			Assert.AreEqual (3, MiniUsers.Count ().Result);
		}

		//FindById
		[Test]
		public void findById(){
			var usr = new MiniUser {
				email = "newMiniUser25@g.com",
				password = "1234"
			};
			usr = MiniUsers.Create (usr).Result;
			usr = MiniUsers.FindById (usr.id).Result;
			Assert.AreNotEqual (null, usr);
			Assert.AreEqual ("newMiniUser25@g.com", usr.email);
			MiniUsers.DeleteById (usr.id).Wait();
		}

		//Create and Delete
		[Test]
		public void createDelete(){
			var newMiniUser = new MiniUser {
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
                var restException = (RestException)e.InnerException;
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
                var restException = (RestException)e.InnerException;
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
		public void exists(){
			Assert.AreEqual(true, MiniUsers.Exists("1").Result);
			Assert.AreEqual(false, MiniUsers.Exists("10").Result);
		}

		//FindOne
		[Test]
		public void findOne(){
			MiniUser usr = MiniUsers.FindOne ().Result;
			Assert.AreNotEqual(null, usr);
			Assert.AreEqual ("1", usr.getID());

			usr = MiniUsers.FindOne("{\"where\": {\"email\" : \"admin1@g.com\"}}").Result;
			Assert.AreNotEqual(null, usr);
			Assert.AreEqual ("2", usr.getID());


		}
		//updateAllById
		[Test]
		public void updateAll_ById(){
			var upUsr = new MiniUser {
				email = "updateAll@g.com"
			};
			var newIds = new List<string> ();
			string filter = "{\"or\": [";
			for (int i = 0; i < 4; i++) {
				var newMiniUser = new MiniUser {
					email = "newMiniUserA" + i + "@g.com",
					password = "1234"
				};
				newMiniUser = MiniUsers.Create (newMiniUser).Result;
				newIds.Add (newMiniUser.id);
				filter = filter + "{\"id\": \"" + newMiniUser.id + "\"}";
				if (i < 3) {
					filter = filter + " ,";
				}
			}
			filter = filter + "]}";

			MiniUsers.UpdateAll(upUsr, filter).Wait();
			MiniUser upRes;
			foreach (string id in newIds) {
				upRes = MiniUsers.FindById (id).Result;
				Assert.AreEqual (upUsr.email, upRes.email);
			}

			upUsr = new MiniUser {
				email = "admin31@g.com"
			};
			MiniUsers.UpdateById (newIds[0], upUsr).Wait();

			upRes = MiniUsers.FindById (newIds[0]).Result;
			Assert.AreEqual (upUsr.email, upRes.email);
			foreach (string id in newIds) {
				MiniUsers.DeleteById (id);
			}
		}
		#endregion
	}
}

