var users = [
  {email: 'admin@g.com', password: '1234'},
  {email: 'admin1@g.com', password: '1234'},
  {email: 'admin2@g.com', password: '1234'}
];

module.exports = function(server) {
  var dataSource = server.dataSources.db;
  dataSource.automigrate('User', function(er) {
    if (er) throw er;
    var Model = server.models.miniUser;
    var Role = server.models.Role;
    var RoleMapping = server.models.RoleMapping;
    var AccessToken = server.models.AccessToken;
    RoleMapping.belongsTo(Model, {as: 'user', foreignKey: 'principalId'});
    AccessToken.belongsTo(Model, {as: 'user', foreignKey: 'userId'});
    //create sample data
    var count = users.length;
    users.forEach(function(user) {
      Model.create(user, function(er, result) {
        if (er) return;

        Role.create({
      name: 'admin'
    }, function(err, role) {
      if (err) console.log('error');

      role.principals.create({
        principalType: RoleMapping.USER,
        principalId: '1'
      }, function(err, principal) {
        if (err) console.log('error');
      });
    });
        console.log('Record created:', result);
        count--;
        if (count === 0) {
          console.log('done');
        }
      });
    });
  });
};
