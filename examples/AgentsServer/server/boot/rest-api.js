// Copyright IBM Corp. 2015. All Rights Reserved.
// Node module: loopback-sdk-xm
// This file is licensed under the MIT License.
// License text available at https://opensource.org/licenses/MIT

module.exports = function mountRestApi(server) {
  var restApiRoot = server.get('restApiRoot');
  server.use(restApiRoot, server.loopback.rest());
};
