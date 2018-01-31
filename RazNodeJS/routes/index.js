var express = require('express');
var router = express.Router();
var env = process.env.Environment || "'Environment' App Setting not found";

/* GET home page. */
router.get('/', function(req, res, next) {
    res.render('index', { title: 'Express', environment: env });
});

module.exports = router;