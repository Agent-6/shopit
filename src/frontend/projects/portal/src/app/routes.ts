export const APP_ROUTES = {
  home: {
    path: '',
    url: '/',
  },
  users: {
    list: {
      path: 'users',
      url: '/users',
    },
    detail: {
      path: 'users/:id',
      url: (id: string) => `/users/${id}`,
    },
  },
  tenants: {
    list: {
      path: 'tenants',
      url: '/tenants',
    },
    detail: {
      path: 'tenants/:id',
      url: (id: string) => `/tenants/${id}`,
    },
  },
};
