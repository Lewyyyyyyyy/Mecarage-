import { App } from './app';

describe('App', () => {
  it('should be instantiable', () => {
    const app = new App();
    expect(app).toBeTruthy();
  });

  it('should expose the App class', () => {
    expect(App).toBeDefined();
  });
});
