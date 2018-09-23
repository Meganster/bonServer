FROM microsoft/dotnet:latest

MAINTAINER Van Yury
RUN apt-get -y update
ADD ./ $WORK/
EXPOSE 80
WORKDIR $WORK/tphighload_2k18

RUN dotnet restore && dotnet build -c Release
USER root
WORKDIR $WORK/tphighload_2k18/server/bin/Release/netcoreapp2.1
CMD dotnet server.dll "/etc/httpd.conf"
