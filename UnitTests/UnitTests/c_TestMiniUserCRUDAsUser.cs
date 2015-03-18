using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;

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
			Assert.AreEqual (-1, MiniUsers.Count ().Result);
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
			resMiniUser = MiniUsers.Create (newMiniUser).Result;
			Assert.AreEqual (null, resMiniUser);
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

			usr = MiniUsers.FindById ("1").Result;
			Assert.AreEqual (null, usr);

		}

		//Find
		[Test]
		public void a13_find(){
			IList<MiniUser> usrList = MiniUsers.Find ().Result;
			Assert.AreEqual (null, usrList);
		}

		//Exists
		[Test]
		public void a14_exists(){
			Assert.AreEqual(false, MiniUsers.Exists("1").Result);
			Assert.AreEqual(false, MiniUsers.Exists("2").Result);
		}

		//FindOne
		[Test]
		public void a15_findOne(){
			Assert.AreEqual(null, MiniUsers.FindOne().Result);
		}

		//update all
		[Test]
		public void a16_updateAll(){
			MiniUser upUsr = new MiniUser () {
				email = "update@g.com"
			};
			MiniUsers.UpdateAll(upUsr, "{\"id\" : \"3\"}").Wait();

			performLoginAsAdmin ();

			MiniUser upRes = MiniUsers.FindById ("3").Result;
			Assert.AreNotEqual (upUsr.email, upRes.email);

			performLogin ();
		}

		//updateById
		[Test]
		public void a17_updateById(){
			MiniUser upUsr = new MiniUser () {
				email = "update@g.com"
			};
			MiniUsers.UpdateById ("2", upUsr).Wait();
			MiniUsers.UpdateById ("3", upUsr).Wait();
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
			MiniUsers.DeleteById ("3").Wait();
			performLoginAsAdmin ();

			MiniUser delRes = MiniUsers.FindById ("3").Result;
			Assert.AreNotEqual (null, delRes);
			performLogin ();
		}
		#endregion
	}
}

