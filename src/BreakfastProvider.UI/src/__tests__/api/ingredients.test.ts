import { getMilk, getGoatMilk, getEggs, getFlour } from '@/lib/api/endpoints/ingredients';

describe('Ingredients API', () => {
  it('should fetch milk', async () => {
    const result = await getMilk();
    expect(result.milk).toBeDefined();
  });

  it('should fetch goat milk', async () => {
    const result = await getGoatMilk();
    expect(result.goatMilk).toBeDefined();
  });

  it('should fetch eggs', async () => {
    const result = await getEggs();
    expect(result.eggs).toBeDefined();
  });

  it('should fetch flour', async () => {
    const result = await getFlour();
    expect(result.flour).toBeDefined();
  });

  it('should fetch all ingredients in parallel', async () => {
    const [milk, goatMilk, eggs, flour] = await Promise.all([
      getMilk(),
      getGoatMilk(),
      getEggs(),
      getFlour(),
    ]);

    expect(milk.milk).toBeDefined();
    expect(goatMilk.goatMilk).toBeDefined();
    expect(eggs.eggs).toBeDefined();
    expect(flour.flour).toBeDefined();
  });
});
