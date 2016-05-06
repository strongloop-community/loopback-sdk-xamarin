// Copyright IBM Corp. 2015. All Rights Reserved.
// Node module: loopback-sdk-xm
// This file is licensed under the MIT License.
// License text available at https://opensource.org/licenses/MIT

var customers = [
  {name: 'Customer A', age: 21},
  {name: 'Customer B', age: 22},
  {name: 'Customer C', age: 23},
  {name: 'Customer D', age: 24},
  {name: 'Customer E', age: 25}
];

module.exports = function(server) {
  var dataSource = server.dataSources.db;
  var Model = server.models.Customer;
  //define a custom scope
  Model.scope('youngFolks', {where: {age: {lte: 22 }}});
  dataSource.automigrate('Customer', function(er) {
    if (er) throw er;
    //create sample data
    var count = customers.length;
    customers.forEach(function(customer) {
      Model.create(customer, function(er, result) {
        if (er) return;
        console.log('Record created:', result);
        count--;
        if (count === 0) {
          console.log('done');
        }
      });
    });
  });
};
