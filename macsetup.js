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
	exec("ruby -e '$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)'", outF);
	exec("brew install https://raw.githubusercontent.com/tjanczuk/edge/master/tools/mono64.rb", outF);
	exec("brew install pkg-config", outF);
} else {
	console.log('> On windows, not running MacSetup.');
}