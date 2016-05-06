// Copyright IBM Corp. 2015. All Rights Reserved.
// Node module: loopback-sdk-xm
// This file is licensed under the MIT License.
// License text available at https://opensource.org/licenses/MIT

module.exports = function(server) {
  var router = server.loopback.Router();
  //router.get('/', function(req, res) {
  //  res.render('index');
  //});

 router.get('/', server.loopback.status());
  server.use(router);
};
