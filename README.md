# HTMLConverterFlow
This function app uses dotliquid to ingest MS Planner payloads sent from Power Automation and reformat them into sensible HTML reports.
It could be repurposed as an intermediary for any Power Automate report generation that requires HTML output.

Just deploy to functions and call it over Https from power automate, you will need a public azure storage account to host the templates used.
