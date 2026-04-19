export const APP_ROUTES = {
  home: {
    path: '',
    url: '/',
  },
  users : {
    list: {
      path: 'users',
      url: '/users',
    },
    detail: {
      path: 'users/:id',
      url: (id: string) => `/users/${id}`,
    },
  },
}
