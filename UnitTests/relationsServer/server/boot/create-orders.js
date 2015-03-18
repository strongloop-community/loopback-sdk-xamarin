var orders = [
  {description: 'Order A', total: 200.45, customerId: 1},
  {description: 'Order B', total: 100,    customerId: 1},
  {description: 'Order C', total: 350.45, customerId: 1},
  {description: 'Order D', total: 150.45, customerId: 2},
  {description: 'Order E', total: 10}
];

module.exports = function(server) {
  var dataSource = server.dataSources.db;
  dataSource.automigrate('Order', function(er) {
    if (er) throw er;
    var Model = server.models.Order;
    //create sample data
    var count = orders.length;
    orders.forEach(function(order) {
      Model.create(order, function(er, result) {
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
