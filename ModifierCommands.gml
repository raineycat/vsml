function vsml_mod_get(name) {
 return variable_instance_get(cc, "mod_" + name);
}

function vsml_mod_set(name, value) {
  variable_instance_set(cc, "mod_" + name, value);
  return value;
}

function vsml_mod_set_proxied(name, proxy, value) {
  var g = cc.gimmick;
  debug("Gimmick: ", g);
  var p = ds_list_find_value(g.proxies, proxy);
  debug("Proxy: ", p);
  variable_instance_set(p, name, value);
  return value;
}
