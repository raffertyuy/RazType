<html>
<head>
<Title>Registration Form (read-only)</Title>
<style type="text/css">
    body { background-color: #fff; border-top: solid 10px #000;
        color: #333; font-size: .85em; margin: 20; padding: 20;
        font-family: "Segoe UI", Verdana, Helvetica, Sans-Serif;
    }
    h1, h2, h3,{ color: #000; margin-bottom: 0; padding-bottom: 0; }
    h1 { font-size: 2em; }
    h2 { font-size: 1.75em; }
    h3 { font-size: 1.2em; }
    table { margin-top: 0.75em; }
    th { font-size: 1.2em; text-align: left; border: none; padding-left: 0; }
    td { padding: 0.25em 2em 0.25em 0em; border: 0 none; }
</style>
</head>
<body>
<?php
	// DB connection info
	$primaryhost = "razsql.database.windows.net,1433";
	$secondaryhost = "raz-sqlsvr.database.windows.net,1433";
	$user = "razadmin";
	$pwd = "Pass123!";
	$db = "razphpsql_db";
	// Connect to database.
	
	try {
		$conn = new PDO( "sqlsrv:Server= $primaryhost ; Database = $db ", $user, $pwd);
		$conn->setAttribute( PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION );
	}
	catch(Exception $e){
		try {
			$conn = new PDO( "sqlsrv:Server= $secondaryhost ; Database = $db ", $user, $pwd);
			$conn->setAttribute( PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION );
		}
		catch(Exception $ex){
			die(var_dump($ex));
		}
	}
	
	$sql_select = "SELECT * FROM registration_tbl";
	$stmt = $conn->query($sql_select);
	$registrants = $stmt->fetchAll();
	
	
	if(count($registrants) > 0) {
		echo "<h2>People who are registered:</h2>";
		echo "<table>";
		echo "<tr><th>Name</th>";
		echo "<th>Email</th>";
		echo "<th>Date</th></tr>";
		foreach($registrants as $registrant) {
			echo "<tr><td>".$registrant['name']."</td>";
			echo "<td>".$registrant['email']."</td>";
			echo "<td>".$registrant['date']."</td></tr>";
		}
		echo "</table>";
	} else {
		echo "<h3>No one is currently registered.</h3>";
	}
?>
</body>
</html>