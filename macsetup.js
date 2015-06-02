var sys = require('sys');
var exec = require('child_process').exec;

outF = function (error, stdout, stderr) {
	if(stdout) {
		sys.print(stdout);
	}
	if (error !== null) {
		console.log(error.toString());
	}
};

if(process.platform !== 'win32') {
	console.log('> Installing MacOS dependencies');
	console.log('> Authorizing access to /usr/local and /Library (Needed for Homebrew and Mono64 installation)');
	exec("sudo chown -R $USER /usr/local /Library", function(error, stdout, stderr) {
		console.log('> Installing MacOS dependencies 1/3: Homebrew');
		exec("ruby -e \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)\"", function(error, stdout, stderr) {
			outF(error, stdout, stderr);
			console.log('> Installing MacOS dependencies 2/3: Mono64. This might take a while.');
			exec("brew install https://raw.githubusercontent.com/tjanczuk/edge/master/tools/mono64.rb", function(error, stdout, stderr) {
				outF(error, stdout, stderr);
				console.log('> Installing MacOS dependencies 3/3: pkg-config');
				exec("brew install pkg-config", function(error, stdout, stderr) {
					outF(error, stdout, stderr);
					console.log('> Done installing MacOS dependencies.');
				});			
			});
		});
	});
} else {
	console.log('> On windows, not running MacSetup.');
}