import { GraphQLClient, gql } from 'graphql-request';
import type {
  Connection,
  OrderSummary,
  RecipeReport,
  IngredientUsage,
  RecipeTypeCount,
} from '../types';

const graphqlClient = new GraphQLClient(
  typeof window !== 'undefined'
    ? `${window.location.origin}/api/graphql`
    : `${process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000'}/api/graphql`,
);

const ORDER_SUMMARIES_QUERY = gql`
  query OrderSummaries($first: Int, $after: String, $where: OrderSummaryFilterInput) {
    orderSummaries(first: $first, after: $after, where: $where, order: { createdAt: DESC }) {
      edges {
        node {
          id
          orderId
          customerName
          itemCount
          tableNumber
          status
          createdAt
        }
        cursor
      }
      pageInfo {
        hasNextPage
        hasPreviousPage
        startCursor
        endCursor
      }
    }
  }
`;

const RECIPE_REPORTS_QUERY = gql`
  query RecipeReports($first: Int, $where: RecipeReportFilterInput) {
    recipeReports(first: $first, where: $where) {
      edges {
        node {
          id
          orderId
          recipeType
          ingredients
          toppings
          loggedAt
        }
      }
    }
  }
`;

const INGREDIENT_USAGE_QUERY = gql`
  query IngredientUsage {
    ingredientUsage {
      ingredient
      count
    }
  }
`;

const POPULAR_RECIPES_QUERY = gql`
  query PopularRecipes {
    popularRecipes {
      recipeType
      count
    }
  }
`;

interface OrderSummariesResult {
  orderSummaries: Connection<OrderSummary>;
}

export async function getOrderSummaries(
  first = 10,
  after?: string,
  statusFilter?: string,
): Promise<Connection<OrderSummary>> {
  const variables = {
    first,
    after,
    where: statusFilter ? { status: { eq: statusFilter } } : undefined,
  };
  const data = await graphqlClient.request<OrderSummariesResult>(
    ORDER_SUMMARIES_QUERY,
    variables,
  );
  return data.orderSummaries;
}

interface RecipeReportsResult {
  recipeReports: Connection<RecipeReport>;
}

export async function getRecipeReports(
  first = 10,
  recipeTypeFilter?: string,
): Promise<Connection<RecipeReport>> {
  const variables = {
    first,
    where: recipeTypeFilter ? { recipeType: { eq: recipeTypeFilter } } : undefined,
  };
  const data = await graphqlClient.request<RecipeReportsResult>(
    RECIPE_REPORTS_QUERY,
    variables,
  );
  return data.recipeReports;
}

interface IngredientUsageResult {
  ingredientUsage: IngredientUsage[];
}

export async function getIngredientUsage(): Promise<IngredientUsage[]> {
  const data = await graphqlClient.request<IngredientUsageResult>(INGREDIENT_USAGE_QUERY);
  return data.ingredientUsage;
}

interface PopularRecipesResult {
  popularRecipes: RecipeTypeCount[];
}

export async function getPopularRecipes(): Promise<RecipeTypeCount[]> {
  const data = await graphqlClient.request<PopularRecipesResult>(POPULAR_RECIPES_QUERY);
  return data.popularRecipes;
}
