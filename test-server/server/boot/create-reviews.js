var reviews = [
  {product: 'Product1', star: 3, authorId: 1},
  {product: 'Product2', star: 4, authorId: 1},
  {product: 'Product3', star: 5, authorId: 1},
  {product: 'Product4', star: 2, authorId: 2},
  {product: 'Product5', star: 5, quthorId: 3}
];

module.exports = function(server) {
  var dataSource = server.dataSources.db;
  dataSource.automigrate('Review', function(er) {
    if (er) throw er;
    var Model = server.models.Review;
    //create sample data
    var count = reviews.length;
    reviews.forEach(function(review) {
      Model.create(review, function(er, result) {
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
