Austrian Bundesamt für Eich- und Vermessungswesen (BEV) has made the Austrian Address Register freely available:

* Data: http://www.bev.gv.at/portal/page?_pageid=713,1604469&_dad=portal&_schema=PORTAL
* TOS: http://www.bev.gv.at/portal/page?_pageid=713,2573888&_dad=portal&_schema=PORTAL

However, the main file (`ADRESSE.csv`) contains three coordinate systems ([EPSG:31254 (Austria GK West)](http://spatialreference.org/ref/epsg/31254/), [EPSG:31255 (Austria GK Central)](http://spatialreference.org/ref/epsg/31255/), [EPSG:31256 (Austria GK East)](http://spatialreference.org/ref/epsg/31256/)) and is too big to be converted to a shapefile directly.

This tool strips all attributes and creates a shapefile in [EPSG:31287 (MGI / Austria Lambert)](http://spatialreference.org/ref/epsg/31287/).
