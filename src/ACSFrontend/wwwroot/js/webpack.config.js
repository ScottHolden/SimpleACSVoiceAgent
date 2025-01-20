const path = require('path');

module.exports = {
    mode: 'development',
    entry: './application.js',
    output: {
        filename: 'app.compiled.js',
        path: path.resolve(__dirname),
    }
};