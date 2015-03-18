module.exports = function(server) {
  var router = server.loopback.Router();
  router.get('/', function(req, res) {
    res.render('index');
  });
  server.use(router);
};
