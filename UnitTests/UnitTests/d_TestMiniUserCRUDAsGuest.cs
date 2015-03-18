using System.Collections.Generic;
using NUnit.Framework;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;

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
			Assert.AreEqual (-1, MiniUsers.Count ().Result);
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
			//tryuing to create duplicate MiniUser
			newMiniUser = MiniUsers.Create (newMiniUser).Result;
			Assert.AreEqual (null, newMiniUser);
		}

		//FindById
		[Test]
		public void a13_findById(){
			MiniUser usr = MiniUsers.FindById ("1").Result;
			Assert.AreEqual (null, usr);
		}

		//Find
		[Test]
		public void a14_find(){
			IList<MiniUser> usrList = MiniUsers.Find ().Result;
			Assert.AreEqual (null, usrList);
		}

		//Exists
		[Test]
		public void a15_exists(){
			Assert.AreEqual(false, MiniUsers.Exists("1").Result);
		}

		//FindOne
		[Test]
		public void a16_findOne(){
			Assert.AreEqual(null, MiniUsers.FindOne().Result);
		}
		#endregion
	}
}

