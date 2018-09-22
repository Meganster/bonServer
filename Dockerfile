FROM microsoft/dotnet:latest
MAINTAINER Van Yury

# Обновление списка пакетов
RUN apt-get -y update

# Копируем исходный код в Docker-контейнер
ADD ./ $WORK/
EXPOSE 80
WORKDIR $WORK/tphighload_2k18
# Собираем проект
RUN dotnet restore && dotnet build -c Release

USER root
WORKDIR $WORK/tphighload_2k18/server/bin/Release/netcoreapp2.1
# Запуск
CMD dotnet server.dll "/etc/httpd.conf"
