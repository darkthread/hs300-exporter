docker build -t hs300-exporter .
docker save hs300-exporter -o .\hs300-exporter.tar  
scp .\hs300-exporter.tar jeffrey@debian12:~/dockers/hs300-exporter