# Wait for Grafana to be ready
until curl -s -o /dev/null -I -w "%{http_code}" http://localhost:3000/api/health -m 10 | grep 200; do
  echo "Waiting for Grafana to be ready..."
  sleep 5
done

curl -X POST -H "Content-Type: application/json" -H "Authorization: Basic $(echo -n "admin:$GRAFANA_PASS" | base64)" -d '{"name":"'$GRAFANA_USER_USERNAME'", "email":"'$GRAFANA_USER_EMAIL'", "login":"'$GRAFANA_USER_USERNAME'", "password":"'$GRAFANA_USER_PASSWORD'"}' http://localhost:3000/api/admin/users

echo "New Grafana user created successfully!"